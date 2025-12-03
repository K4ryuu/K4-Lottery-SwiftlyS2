namespace K4_Lottery.Config;

public sealed class LotteryConfig
{
	public string DatabaseConnection { get; set; } = "default";
	public string WalletKind { get; set; } = "credits";
	public long TicketPrice { get; set; } = 100;
	public int MaxTicketsPerPlayer { get; set; } = 10;
	public int DrawIntervalDays { get; set; } = 7;
	public double WinnerPercentage { get; set; } = 75.0;
	public string DrawTime { get; set; } = "20:00";
}
