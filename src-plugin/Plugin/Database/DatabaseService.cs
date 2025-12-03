using System.Data;
using Dommel;
using K4_Lottery.Database.Migrations;
using Microsoft.Extensions.Logging;

namespace K4_Lottery.Database;

public sealed class DatabaseService
{
	private readonly string _connectionName;

	public DatabaseService(string connectionName)
	{
		_connectionName = connectionName;
	}

	public bool IsEnabled { get; private set; }

	public async Task InitializeAsync()
	{
		try
		{
			await Task.Delay(500);
			using var connection = Plugin.Core.Database.GetConnection(_connectionName);
			MigrationRunner.RunMigrations(connection);
			IsEnabled = true;
			Plugin.Core.Logger.LogInformation("K4-Lottery database initialized successfully.");
		}
		catch (Exception ex)
		{
			Plugin.Core.Logger.LogError(ex, "Failed to initialize K4-Lottery database.");
			IsEnabled = false;
		}
	}

	private IDbConnection GetConnection() => Plugin.Core.Database.GetConnection(_connectionName);

	public async Task<LotteryDraw?> GetCurrentDrawAsync()
	{
		using var conn = GetConnection();
		var draws = await conn.SelectAsync<LotteryDraw>(d => !d.IsCompleted);
		return draws.OrderByDescending(d => d.Id).FirstOrDefault();
	}

	public async Task<LotteryDraw> CreateDrawAsync(DateTime startDate, DateTime endDate)
	{
		using var conn = GetConnection();
		var draw = new LotteryDraw
		{
			StartDate = startDate,
			EndDate = endDate,
			TotalPot = 0,
			IsCompleted = false,
			CreatedAt = DateTime.UtcNow
		};
		draw.Id = (int)(await conn.InsertAsync(draw))!;
		return draw;
	}

	public async Task<LotteryTicket?> GetPlayerTicketAsync(int drawId, ulong steamId)
	{
		using var conn = GetConnection();
		var tickets = await conn.SelectAsync<LotteryTicket>(t => t.DrawId == drawId && t.SteamId == steamId);
		return tickets.FirstOrDefault();
	}

	public async Task<int> GetTotalTicketsAsync(int drawId)
	{
		using var conn = GetConnection();
		var tickets = await conn.SelectAsync<LotteryTicket>(t => t.DrawId == drawId);
		return tickets.Sum(t => t.TicketCount);
	}

	public async Task<int> GetParticipantCountAsync(int drawId)
	{
		using var conn = GetConnection();
		var tickets = await conn.SelectAsync<LotteryTicket>(t => t.DrawId == drawId);
		return tickets.Select(t => t.SteamId).Distinct().Count();
	}

	public async Task BuyTicketsAsync(int drawId, ulong steamId, string playerName, int count, long totalCost)
	{
		using var conn = GetConnection();

		var existingTickets = await conn.SelectAsync<LotteryTicket>(t => t.DrawId == drawId && t.SteamId == steamId);
		var existing = existingTickets.FirstOrDefault();

		if (existing != null)
		{
			existing.TicketCount += count;
			existing.PlayerName = playerName;
			await conn.UpdateAsync(existing);
		}
		else
		{
			var ticket = new LotteryTicket
			{
				DrawId = drawId,
				SteamId = steamId,
				PlayerName = playerName,
				TicketCount = count,
				PurchasedAt = DateTime.UtcNow
			};
			await conn.InsertAsync(ticket);
		}

		var draw = await conn.GetAsync<LotteryDraw>(drawId);
		if (draw != null)
		{
			draw.TotalPot += totalCost;
			await conn.UpdateAsync(draw);
		}

		await UpdateHistorySpentAsync(conn, steamId, playerName, count, totalCost);
	}

	private static async Task UpdateHistorySpentAsync(IDbConnection conn, ulong steamId, string playerName, int tickets, long spent)
	{
		var histories = await conn.SelectAsync<LotteryHistory>(h => h.SteamId == steamId);
		var existing = histories.FirstOrDefault();

		if (existing != null)
		{
			existing.TotalSpent += spent;
			existing.TicketsBought += tickets;
			existing.PlayerName = playerName;
			await conn.UpdateAsync(existing);
		}
		else
		{
			var history = new LotteryHistory
			{
				SteamId = steamId,
				PlayerName = playerName,
				TotalSpent = spent,
				TicketsBought = tickets
			};
			await conn.InsertAsync(history);
		}
	}

	public async Task<List<LotteryTicket>> GetAllTicketsAsync(int drawId)
	{
		using var conn = GetConnection();
		var tickets = await conn.SelectAsync<LotteryTicket>(t => t.DrawId == drawId);
		return tickets.ToList();
	}

	public async Task CompleteDrawAsync(int drawId, ulong winnerSteamId, string winnerName, long winnerAmount)
	{
		using var conn = GetConnection();

		var draw = await conn.GetAsync<LotteryDraw>(drawId);
		if (draw == null || draw.IsCompleted)
			return;

		draw.IsCompleted = true;
		draw.WinnerSteamId = winnerSteamId > 0 ? winnerSteamId : null;
		draw.WinnerName = winnerSteamId > 0 ? winnerName : null;
		draw.WinnerAmount = winnerSteamId > 0 ? winnerAmount : null;
		await conn.UpdateAsync(draw);

		if (winnerSteamId > 0 && winnerAmount > 0)
		{
			var histories = await conn.SelectAsync<LotteryHistory>(h => h.SteamId == winnerSteamId);
			var existing = histories.FirstOrDefault();

			if (existing != null)
			{
				existing.TotalWon += winnerAmount;
				existing.WinsCount += 1;
				await conn.UpdateAsync(existing);
			}
			else
			{
				var history = new LotteryHistory
				{
					SteamId = winnerSteamId,
					PlayerName = winnerName,
					TotalWon = winnerAmount,
					WinsCount = 1
				};
				await conn.InsertAsync(history);
			}
		}

		var ticketsToDelete = await conn.SelectAsync<LotteryTicket>(t => t.DrawId == drawId);
		foreach (var ticket in ticketsToDelete)
			await conn.DeleteAsync(ticket);

		var completedDraws = await conn.SelectAsync<LotteryDraw>(d => d.IsCompleted);
		var oldDraws = completedDraws.OrderByDescending(d => d.Id).Skip(5);
		foreach (var oldDraw in oldDraws)
			await conn.DeleteAsync(oldDraw);
	}

	public async Task<List<LotteryHistory>> GetTopWinnersAsync(int limit = 10)
	{
		using var conn = GetConnection();
		var histories = await conn.SelectAsync<LotteryHistory>(h => h.TotalWon > 0);
		return histories.OrderByDescending(h => h.TotalWon).Take(limit).ToList();
	}

	public async Task<List<LotteryDraw>> GetRecentDrawsAsync(int limit = 5)
	{
		using var conn = GetConnection();
		var draws = await conn.SelectAsync<LotteryDraw>(d => d.IsCompleted);
		return draws.OrderByDescending(d => d.Id).Take(limit).ToList();
	}
}
