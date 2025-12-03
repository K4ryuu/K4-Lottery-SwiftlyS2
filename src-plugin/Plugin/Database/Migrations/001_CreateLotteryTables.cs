using FluentMigrator;

namespace K4_Lottery.Database.Migrations;

[Migration(2025122030)]
public class CreateLotteryTables : Migration
{
	public override void Up()
	{
		if (!Schema.Table("k4_lottery_draws").Exists())
		{
			Create.Table("k4_lottery_draws")
				.WithColumn("id").AsInt32().PrimaryKey().Identity()
				.WithColumn("start_date").AsDateTime().NotNullable()
				.WithColumn("end_date").AsDateTime().NotNullable()
				.WithColumn("total_pot").AsInt64().NotNullable().WithDefaultValue(0)
				.WithColumn("winner_steam_id").AsInt64().Nullable()
				.WithColumn("winner_name").AsString(128).Nullable()
				.WithColumn("winner_amount").AsInt64().Nullable()
				.WithColumn("is_completed").AsBoolean().NotNullable().WithDefaultValue(false)
				.WithColumn("created_at").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime);

			Create.Index("ix_lottery_draws_completed").OnTable("k4_lottery_draws").OnColumn("is_completed");
			Create.Index("ix_lottery_draws_end_date").OnTable("k4_lottery_draws").OnColumn("end_date");
		}

		if (!Schema.Table("k4_lottery_tickets").Exists())
		{
			Create.Table("k4_lottery_tickets")
				.WithColumn("id").AsInt32().PrimaryKey().Identity()
				.WithColumn("draw_id").AsInt32().NotNullable().ForeignKey("k4_lottery_draws", "id").OnDelete(System.Data.Rule.Cascade)
				.WithColumn("steam_id").AsInt64().NotNullable()
				.WithColumn("player_name").AsString(128).NotNullable()
				.WithColumn("ticket_count").AsInt32().NotNullable().WithDefaultValue(1)
				.WithColumn("purchased_at").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime);

			Create.Index("ix_lottery_tickets_draw").OnTable("k4_lottery_tickets").OnColumn("draw_id");
			Create.Index("ix_lottery_tickets_steam").OnTable("k4_lottery_tickets").OnColumn("steam_id");
			Create.UniqueConstraint("uq_lottery_ticket_player").OnTable("k4_lottery_tickets").Columns("draw_id", "steam_id");
		}

		if (!Schema.Table("k4_lottery_history").Exists())
		{
			Create.Table("k4_lottery_history")
				.WithColumn("id").AsInt32().PrimaryKey().Identity()
				.WithColumn("steam_id").AsInt64().NotNullable()
				.WithColumn("player_name").AsString(128).NotNullable()
				.WithColumn("total_spent").AsInt64().NotNullable().WithDefaultValue(0)
				.WithColumn("total_won").AsInt64().NotNullable().WithDefaultValue(0)
				.WithColumn("wins_count").AsInt32().NotNullable().WithDefaultValue(0)
				.WithColumn("tickets_bought").AsInt32().NotNullable().WithDefaultValue(0);

			Create.Index("ix_lottery_history_steam").OnTable("k4_lottery_history").OnColumn("steam_id").Unique();
		}
	}

	public override void Down()
	{
		Delete.Table("k4_lottery_history");
		Delete.Table("k4_lottery_tickets");
		Delete.Table("k4_lottery_draws");
	}
}
