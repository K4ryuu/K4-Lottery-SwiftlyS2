using System.Security.Cryptography;
using K4_Lottery.Database;
using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared.Players;

namespace K4_Lottery;

public sealed class LotteryService
{
	private readonly Plugin _plugin;
	private readonly DatabaseService _database;
	private DateTime _lastDrawCheck = DateTime.MinValue;

	public LotteryService(Plugin plugin, DatabaseService database)
	{
		_plugin = plugin;
		_database = database;

		Task.Run(EnsureCurrentDrawAsync);
	}

	private async Task EnsureCurrentDrawAsync()
	{
		while (!_database.IsEnabled)
			await Task.Delay(100);

		var draw = await _database.GetCurrentDrawAsync();
		if (draw == null)
		{
			await CreateNewDrawAsync();
			Plugin.Core.Logger.LogInformation("K4-Lottery: Created new lottery draw.");
		}
		else
		{
			Plugin.Core.Logger.LogInformation("K4-Lottery: Existing draw found, ends at {EndDate}.", draw.EndDate);
		}
	}

	private async Task<LotteryDraw> CreateNewDrawAsync()
	{
		var now = DateTime.UtcNow;
		var endDate = CalculateNextDrawDate(now);
		return await _database.CreateDrawAsync(now, endDate);
	}

	private DateTime CalculateNextDrawDate(DateTime from)
	{
		var drawTime = TimeSpan.Parse(_plugin.Lottery.DrawTime);
		var nextDraw = from.Date.Add(drawTime).AddDays(_plugin.Lottery.DrawIntervalDays);

		if (nextDraw <= from)
			nextDraw = nextDraw.AddDays(_plugin.Lottery.DrawIntervalDays);

		return nextDraw;
	}

	public async Task BuyTicketsAsync(IPlayer player, string[] args)
	{
		var localizer = Plugin.Core.Translation.GetPlayerLocalizer(player);

		if (!_database.IsEnabled || _plugin.EconomyAPI == null)
		{
			SendAsync(player, WithPrefix(localizer, localizer["k4.lottery.error.economy_unavailable"]));
			return;
		}

		int count = 1;
		if (args.Length > 0 && int.TryParse(args[0], out var parsed) && parsed > 0)
			count = parsed;

		var draw = await _database.GetCurrentDrawAsync();
		draw ??= await CreateNewDrawAsync();

		var existingTicket = await _database.GetPlayerTicketAsync(draw.Id, player.SteamID);
		var currentCount = existingTicket?.TicketCount ?? 0;

		if (currentCount + count > _plugin.Lottery.MaxTicketsPerPlayer)
		{
			var canBuy = _plugin.Lottery.MaxTicketsPerPlayer - currentCount;
			if (canBuy <= 0)
			{
				SendAsync(player, WithPrefix(localizer, localizer["k4.lottery.error.max_tickets", _plugin.Lottery.MaxTicketsPerPlayer]));
				return;
			}
			count = canBuy;
		}

		var totalCost = count * _plugin.Lottery.TicketPrice;
		var balance = _plugin.EconomyAPI.GetPlayerBalance(player.SteamID, _plugin.Lottery.WalletKind);

		if (balance < totalCost)
		{
			SendAsync(player, WithPrefix(localizer, localizer["k4.lottery.error.insufficient_funds", totalCost, balance]));
			return;
		}

		try
		{
			await _database.BuyTicketsAsync(draw.Id, player.SteamID, player.Controller.PlayerName, count, totalCost);
			_plugin.EconomyAPI.SubtractPlayerBalance(player.SteamID, _plugin.Lottery.WalletKind, (int)totalCost);

			var newTotal = currentCount + count;
			SendAsync(player, WithPrefix(localizer, localizer["k4.lottery.success.bought", count, totalCost, newTotal]));
		}
		catch (Exception ex)
		{
			Plugin.Core.Logger.LogError(ex, "K4-Lottery: Failed to buy tickets for {SteamId}", player.SteamID);
			SendAsync(player, WithPrefix(localizer, localizer["k4.lottery.error.purchase_failed"]));
		}
	}

	public async Task ShowInfoAsync(IPlayer player)
	{
		var localizer = Plugin.Core.Translation.GetPlayerLocalizer(player);

		if (!_database.IsEnabled)
		{
			SendAsync(player, WithPrefix(localizer, localizer["k4.lottery.error.no_draw"]));
			return;
		}

		var draw = await _database.GetCurrentDrawAsync();
		if (draw == null)
		{
			SendAsync(player, WithPrefix(localizer, localizer["k4.lottery.error.no_draw"]));
			return;
		}

		var playerTicket = await _database.GetPlayerTicketAsync(draw.Id, player.SteamID);
		var totalTickets = await _database.GetTotalTicketsAsync(draw.Id);
		var participants = await _database.GetParticipantCountAsync(draw.Id);
		var winnerPot = (long)(draw.TotalPot * _plugin.Lottery.WinnerPercentage / 100);
		var timeLeft = draw.EndDate - DateTime.UtcNow;

		var playerTicketCount = playerTicket?.TicketCount ?? 0;
		var winChance = totalTickets > 0 ? (double)playerTicketCount / totalTickets * 100 : 0;

		Plugin.Core.Scheduler.NextWorldUpdate(() =>
		{
			if (!player.IsValid) return;
			player.SendChat(localizer["k4.lottery.info.header"]);
			player.SendChat(localizer["k4.lottery.info.pot", draw.TotalPot, winnerPot]);
			player.SendChat(localizer["k4.lottery.info.tickets", totalTickets, participants]);
			player.SendChat(localizer["k4.lottery.info.your_tickets", playerTicketCount, _plugin.Lottery.MaxTicketsPerPlayer]);
			player.SendChat(localizer["k4.lottery.info.win_chance", winChance]);
			player.SendChat(localizer["k4.lottery.info.time_left", FormatTimeSpan(localizer, timeLeft)]);
			player.SendChat(localizer["k4.lottery.info.ticket_price", _plugin.Lottery.TicketPrice]);
		});
	}

	public async Task ShowTopAsync(IPlayer player)
	{
		var localizer = Plugin.Core.Translation.GetPlayerLocalizer(player);

		if (!_database.IsEnabled)
			return;

		var topWinners = await _database.GetTopWinnersAsync(10);

		Plugin.Core.Scheduler.NextWorldUpdate(() =>
		{
			if (!player.IsValid) return;
			player.SendChat(localizer["k4.lottery.top.header"]);

			if (topWinners.Count == 0)
			{
				player.SendChat(localizer["k4.lottery.top.empty"]);
				return;
			}

			for (int i = 0; i < topWinners.Count; i++)
			{
				var winner = topWinners[i];
				player.SendChat(localizer["k4.lottery.top.item", i + 1, winner.PlayerName, winner.TotalWon, winner.WinsCount]);
			}
		});
	}

	public async Task ShowHistoryAsync(IPlayer player)
	{
		var localizer = Plugin.Core.Translation.GetPlayerLocalizer(player);

		if (!_database.IsEnabled)
			return;

		var recentDraws = await _database.GetRecentDrawsAsync(5);

		Plugin.Core.Scheduler.NextWorldUpdate(() =>
		{
			if (!player.IsValid) return;
			player.SendChat(localizer["k4.lottery.history.header"]);

			if (recentDraws.Count == 0)
			{
				player.SendChat(localizer["k4.lottery.history.empty"]);
				return;
			}

			for (int i = 0; i < recentDraws.Count; i++)
			{
				var draw = recentDraws[i];
				player.SendChat(localizer["k4.lottery.history.item", i + 1, draw.WinnerName ?? "N/A", draw.WinnerAmount ?? 0, draw.EndDate.ToString("yyyy-MM-dd")]);
			}
		});
	}

	public async Task CheckAndProcessDrawAsync()
	{
		if (!_database.IsEnabled)
			return;

		if (DateTime.UtcNow - _lastDrawCheck < TimeSpan.FromSeconds(30))
			return;

		_lastDrawCheck = DateTime.UtcNow;

		var draw = await _database.GetCurrentDrawAsync();
		if (draw == null || DateTime.UtcNow < draw.EndDate)
			return;

		await ProcessDrawAsync(draw);
	}

	private async Task ProcessDrawAsync(LotteryDraw draw)
	{
		var tickets = await _database.GetAllTicketsAsync(draw.Id);

		if (tickets.Count == 0)
		{
			await _database.CompleteDrawAsync(draw.Id, 0, "No participants", 0);
			await CreateNewDrawAsync();
			return;
		}

		var ticketPool = new List<LotteryTicket>();
		foreach (var ticket in tickets)
		{
			for (int i = 0; i < ticket.TicketCount; i++)
				ticketPool.Add(ticket);
		}

		var winnerIndex = RandomNumberGenerator.GetInt32(ticketPool.Count);
		var winner = ticketPool[winnerIndex];
		var winnerAmount = (long)(draw.TotalPot * _plugin.Lottery.WinnerPercentage / 100);

		await _database.CompleteDrawAsync(draw.Id, winner.SteamId, winner.PlayerName, winnerAmount);

		_plugin.EconomyAPI?.AddPlayerBalance(winner.SteamId, _plugin.Lottery.WalletKind, (int)winnerAmount);

		Plugin.Core.Logger.LogInformation("K4-Lottery: {WinnerName} ({SteamId}) won {Amount} credits (Pot: {Pot})",
			winner.PlayerName, winner.SteamId, winnerAmount, draw.TotalPot);

		AnnounceWinner(winner.PlayerName, winnerAmount, draw.TotalPot);

		await CreateNewDrawAsync();
	}

	private static void AnnounceWinner(string winnerName, long amount, long totalPot)
	{
		Plugin.Core.Scheduler.NextWorldUpdate(() =>
		{
			foreach (var player in Plugin.Core.PlayerManager.GetAllPlayers())
			{
				if (!player.IsValid) continue;
				var localizer = Plugin.Core.Translation.GetPlayerLocalizer(player);
				player.SendChat(localizer["k4.lottery.announce.winner", winnerName, amount, totalPot]);
			}
		});
	}

	private static void SendAsync(IPlayer player, string message)
	{
		Plugin.Core.Scheduler.NextWorldUpdate(() =>
		{
			if (player.IsValid)
				player.SendChat(message);
		});
	}

	private static string WithPrefix(SwiftlyS2.Shared.Translation.ILocalizer localizer, string message)
		=> $"{localizer["k4.general.prefix"]} {message}";

	private static string FormatTimeSpan(SwiftlyS2.Shared.Translation.ILocalizer localizer, TimeSpan ts)
	{
		if (ts.TotalDays >= 1)
			return localizer["k4.lottery.time.days", (int)ts.TotalDays, ts.Hours, ts.Minutes];
		if (ts.TotalHours >= 1)
			return localizer["k4.lottery.time.hours", (int)ts.TotalHours, ts.Minutes];
		return localizer["k4.lottery.time.minutes", ts.Minutes];
	}
}
