using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace K4_Lottery.Database;

[Table("k4_lottery_draws")]
public sealed class LotteryDraw
{
	[Key]
	[Column("id")]
	public int Id { get; set; }

	[Column("start_date")]
	public DateTime StartDate { get; set; }

	[Column("end_date")]
	public DateTime EndDate { get; set; }

	[Column("total_pot")]
	public long TotalPot { get; set; }

	[Column("winner_steam_id")]
	public ulong? WinnerSteamId { get; set; }

	[Column("winner_name")]
	public string? WinnerName { get; set; }

	[Column("winner_amount")]
	public long? WinnerAmount { get; set; }

	[Column("is_completed")]
	public bool IsCompleted { get; set; }

	[Column("created_at")]
	public DateTime CreatedAt { get; set; }
}

[Table("k4_lottery_tickets")]
public sealed class LotteryTicket
{
	[Key]
	[Column("id")]
	public int Id { get; set; }

	[Column("draw_id")]
	public int DrawId { get; set; }

	[Column("steam_id")]
	public ulong SteamId { get; set; }

	[Column("player_name")]
	public string PlayerName { get; set; } = string.Empty;

	[Column("ticket_count")]
	public int TicketCount { get; set; }

	[Column("purchased_at")]
	public DateTime PurchasedAt { get; set; }
}

[Table("k4_lottery_history")]
public sealed class LotteryHistory
{
	[Key]
	[Column("id")]
	public int Id { get; set; }

	[Column("steam_id")]
	public ulong SteamId { get; set; }

	[Column("player_name")]
	public string PlayerName { get; set; } = string.Empty;

	[Column("total_spent")]
	public long TotalSpent { get; set; }

	[Column("total_won")]
	public long TotalWon { get; set; }

	[Column("wins_count")]
	public int WinsCount { get; set; }

	[Column("tickets_bought")]
	public int TicketsBought { get; set; }
}
