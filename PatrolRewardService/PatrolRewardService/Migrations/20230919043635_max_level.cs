using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PatrolRewardService.Migrations
{
    /// <inheritdoc />
    public partial class max_level : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "max_level",
                table: "reward_policies",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "max_level",
                table: "reward_policies");
        }
    }
}
