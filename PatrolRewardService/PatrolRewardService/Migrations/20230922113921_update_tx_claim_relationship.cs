using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PatrolRewardService.Migrations
{
    /// <inheritdoc />
    public partial class update_tx_claim_relationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "update transactions set claim_id = claims.id from claims where claims.tx_id = transactions.tx_id;");
            migrationBuilder.DropForeignKey(
                name: "fk_claims_transactions_tx_id",
                table: "claims");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_transactions_avatar_address_claim_count",
                table: "transactions");

            migrationBuilder.DropIndex(
                name: "ix_claims_tx_id",
                table: "claims");

            migrationBuilder.DropColumn(
                name: "tx_id",
                table: "claims");

            migrationBuilder.CreateIndex(
                name: "ix_transactions_avatar_address",
                table: "transactions",
                column: "avatar_address");

            migrationBuilder.CreateIndex(
                name: "ix_transactions_claim_id",
                table: "transactions",
                column: "claim_id");

            migrationBuilder.AddForeignKey(
                name: "fk_transactions_claims_claim_id",
                table: "transactions",
                column: "claim_id",
                principalTable: "claims",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_transactions_claims_claim_id",
                table: "transactions");

            migrationBuilder.DropIndex(
                name: "ix_transactions_avatar_address",
                table: "transactions");

            migrationBuilder.DropIndex(
                name: "ix_transactions_claim_id",
                table: "transactions");

            migrationBuilder.AddColumn<string>(
                name: "tx_id",
                table: "claims",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddUniqueConstraint(
                name: "ak_transactions_avatar_address_claim_count",
                table: "transactions",
                columns: new[] { "avatar_address", "claim_count" });

            migrationBuilder.CreateIndex(
                name: "ix_claims_tx_id",
                table: "claims",
                column: "tx_id",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_claims_transactions_tx_id",
                table: "claims",
                column: "tx_id",
                principalTable: "transactions",
                principalColumn: "tx_id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
