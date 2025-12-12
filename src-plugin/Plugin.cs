using Economy.Contract;
using K4_Lottery.Config;
using K4_Lottery.Database;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.Plugins;

namespace K4_Lottery;

[PluginMetadata(
	Id = "k4.lottery",
	Version = "1.0.1",
	Name = "K4 - Lottery",
	Author = "K4ryuu",
	Description = "Lottery system with ticket purchases and periodic draws"
)]
public sealed class Plugin(ISwiftlyCore core) : BasePlugin(core)
{
	private const string LotteryConfigFileName = "config.json";
	private const string LotteryConfigSection = "K4Lottery";
	private const string CommandsConfigFileName = "commands.json";
	private const string CommandsConfigSection = "K4LotteryCommands";

	public static new ISwiftlyCore Core { get; private set; } = null!;
	public static IOptionsMonitor<LotteryConfig> Lottery { get; private set; } = null!;
	public static IOptionsMonitor<CommandsConfig> Commands { get; private set; } = null!;

	private DatabaseService _database = null!;
	private LotteryService _lotteryService = null!;
	private CancellationTokenSource? _drawTimerCts;

	public IEconomyAPIv1? EconomyAPI { get; private set; }

	public override void Load(bool hotReload)
	{
		Core = base.Core;

		InitializeConfigs();
		InitializeDatabase();
		InitializeServices();
		RegisterCommands();
		StartDrawTimer();
	}

	public override void Unload()
	{
		_drawTimerCts?.Cancel();
		_drawTimerCts = null;
	}

	public override void UseSharedInterface(IInterfaceManager interfaceManager)
	{
		if (!interfaceManager.HasSharedInterface("Economy.API.v1"))
		{
			Core.Logger.LogWarning("Economy API is not available.");
			return;
		}

		EconomyAPI = interfaceManager.GetSharedInterface<IEconomyAPIv1>("Economy.API.v1");
		EconomyAPI.EnsureWalletKind(Lottery.CurrentValue.WalletKind);
	}

	private static void InitializeConfigs()
	{
		Core.Configuration
			.InitializeJsonWithModel<LotteryConfig>(LotteryConfigFileName, LotteryConfigSection)
			.Configure(builder =>
			{
				builder.AddJsonFile(LotteryConfigFileName, optional: false, reloadOnChange: true);
			});

		Core.Configuration
			.InitializeJsonWithModel<CommandsConfig>(CommandsConfigFileName, CommandsConfigSection)
			.Configure(builder =>
			{
				builder.AddJsonFile(CommandsConfigFileName, optional: false, reloadOnChange: true);
			});

		ServiceCollection services = new();
		services.AddSwiftly(Core)
			.AddOptions<LotteryConfig>()
			.BindConfiguration(LotteryConfigFileName);

		services.AddOptions<CommandsConfig>()
			.BindConfiguration(CommandsConfigFileName);

		var provider = services.BuildServiceProvider();
		Lottery = provider.GetRequiredService<IOptionsMonitor<LotteryConfig>>();
		Commands = provider.GetRequiredService<IOptionsMonitor<CommandsConfig>>();
	}

	private void InitializeDatabase()
	{
		_database = new DatabaseService(Lottery.CurrentValue.DatabaseConnection);
		Task.Run(async () => await _database.InitializeAsync());
	}

	private void InitializeServices()
	{
		_lotteryService = new LotteryService(this, _database);
		_lotteryService.Initialize();
	}

	private void RegisterCommands()
	{
		Core.Command.RegisterCommand(Commands.CurrentValue.Lottery.Name, OnLotteryCommand);
		foreach (var alias in Commands.CurrentValue.Lottery.Aliases)
			Core.Command.RegisterCommand(alias, OnLotteryCommand);
	}

	private void OnLotteryCommand(ICommandContext cmdCtx)
	{
		var player = cmdCtx.Sender;
		if (player == null || !player.IsValid) return;

		var args = cmdCtx.Args.ToArray();

		if (args.Length == 0)
		{
			ShowHelp(player);
			return;
		}

		var subCommand = args[0].ToLowerInvariant();
		var subArgs = args.Skip(1).ToArray();

		if (IsCommand(subCommand, Commands.CurrentValue.Buy))
			Task.Run(async () => await _lotteryService.BuyTicketsAsync(player, subArgs));
		else if (IsCommand(subCommand, Commands.CurrentValue.Info))
			Task.Run(async () => await _lotteryService.ShowInfoAsync(player));
		else if (IsCommand(subCommand, Commands.CurrentValue.Top))
			Task.Run(async () => await _lotteryService.ShowTopAsync(player));
		else if (IsCommand(subCommand, Commands.CurrentValue.History))
			Task.Run(async () => await _lotteryService.ShowHistoryAsync(player));
		else
			ShowHelp(player);
	}

	private static bool IsCommand(string input, CommandDefinition cmd)
		=> input == cmd.Name || cmd.Aliases.Contains(input);

	private void ShowHelp(SwiftlyS2.Shared.Players.IPlayer player)
	{
		var localizer = Core.Translation.GetPlayerLocalizer(player);
		player.SendChat(localizer["k4.lottery.help.header"]);
		player.SendChat(localizer["k4.lottery.help.buy"]);
		player.SendChat(localizer["k4.lottery.help.info"]);
		player.SendChat(localizer["k4.lottery.help.top"]);
		player.SendChat(localizer["k4.lottery.help.history"]);
		player.SendChat(localizer["k4.lottery.help.footer"]);
	}

	private void StartDrawTimer()
	{
		_drawTimerCts = Core.Scheduler.RepeatBySeconds(60, CheckDrawTime);
	}

	private void CheckDrawTime()
	{
		Task.Run(async () => await _lotteryService.CheckAndProcessDrawAsync());
	}

	internal DatabaseService GetDatabase() => _database;
}
