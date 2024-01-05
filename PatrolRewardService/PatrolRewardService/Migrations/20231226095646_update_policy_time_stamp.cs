using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PatrolRewardService.Migrations
{
    /// <inheritdoc />
    public partial class update_policy_time_stamp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ended_at",
                table: "reward_policies",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: DateTime.MaxValue);

            migrationBuilder.AddColumn<DateTime>(
                name: "started_at",
                table: "reward_policies",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ended_at",
                table: "reward_policies");

            migrationBuilder.DropColumn(
                name: "started_at",
                table: "reward_policies");
        }
    }
}
