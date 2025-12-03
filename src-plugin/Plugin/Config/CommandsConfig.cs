namespace K4_Lottery.Config;

public sealed class CommandsConfig
{
	public CommandDefinition Lottery { get; set; } = new()
	{
		Name = "lottery",
		Aliases = []
	};

	public CommandDefinition Buy { get; set; } = new() { Name = "buy" };
	public CommandDefinition Info { get; set; } = new() { Name = "info" };
	public CommandDefinition Top { get; set; } = new() { Name = "top" };
	public CommandDefinition History { get; set; } = new() { Name = "history" };
}

public sealed class CommandDefinition
{
	public string Name { get; set; } = string.Empty;
	public List<string> Aliases { get; set; } = [];
}
