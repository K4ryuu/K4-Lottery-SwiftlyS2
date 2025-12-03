using System.Data;
using FluentMigrator.Runner;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using MySqlConnector;
using Npgsql;

namespace K4_Lottery.Database.Migrations;

public static class MigrationRunner
{
	public static void RunMigrations(IDbConnection dbConnection)
	{
		var serviceProvider = new ServiceCollection()
			.AddFluentMigratorCore()
			.ConfigureRunner(rb =>
			{
				ConfigureDatabase(rb, dbConnection);
				rb.ScanIn(typeof(MigrationRunner).Assembly).For.Migrations();
			})
			.AddLogging(lb => lb.AddFluentMigratorConsole())
			.BuildServiceProvider(false);

		using var scope = serviceProvider.CreateScope();
		var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
		runner.MigrateUp();
	}

	private static void ConfigureDatabase(IMigrationRunnerBuilder rb, IDbConnection dbConnection)
	{
		switch (dbConnection)
		{
			case MySqlConnection:
				rb.AddMySql5();
				break;
			case NpgsqlConnection:
				rb.AddPostgres();
				break;
			case SqliteConnection:
				rb.AddSQLite();
				break;
			default:
				throw new NotSupportedException($"Unsupported database connection type: {dbConnection.GetType().Name}");
		}

		rb.WithGlobalConnectionString(dbConnection.ConnectionString);
	}
}
