using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PatrolRewardService.Migrations
{
    /// <inheritdoc />
    public partial class init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "avatars",
                columns: table => new
                {
                    avatar_address = table.Column<string>(type: "text", nullable: false),
                    agent_address = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_claimed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    level = table.Column<int>(type: "integer", nullable: false),
                    claim_count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_avatars", x => x.avatar_address);
                });

            migrationBuilder.CreateTable(
                name: "reward_policies",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    activate = table.Column<bool>(type: "boolean", nullable: false),
                    free = table.Column<bool>(type: "boolean", nullable: false),
                    minimum_level = table.Column<int>(type: "integer", nullable: false),
                    minimum_required_interval = table.Column<TimeSpan>(type: "interval", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reward_policies", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "rewards",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    per_interval = table.Column<int>(type: "integer", nullable: false),
                    reward_interval = table.Column<TimeSpan>(type: "interval", nullable: false),
                    reward_type = table.Column<string>(type: "text", nullable: false),
                    currency = table.Column<string>(type: "text", nullable: true),
                    ticker = table.Column<string>(type: "text", nullable: true),
                    fungible_id = table.Column<string>(type: "text", nullable: true),
                    item_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rewards", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "transactions",
                columns: table => new
                {
                    tx_id = table.Column<string>(type: "text", nullable: false),
                    avatar_address = table.Column<string>(type: "text", nullable: false),
                    nonce = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    payload = table.Column<string>(type: "text", nullable: false),
                    claim_id = table.Column<int>(type: "integer", nullable: false),
                    claim_count = table.Column<int>(type: "integer", nullable: false),
                    result = table.Column<string>(type: "text", nullable: false),
                    gas = table.Column<int>(type: "integer", nullable: true),
                    gas_limit = table.Column<long>(type: "bigint", nullable: true),
                    exception_name = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_transactions", x => x.tx_id);
                    table.UniqueConstraint("ak_transactions_avatar_address_claim_count", x => new { x.avatar_address, x.claim_count });
                    table.UniqueConstraint("ak_transactions_nonce", x => x.nonce);
                    table.ForeignKey(
                        name: "fk_transactions_avatars_avatar_address1",
                        column: x => x.avatar_address,
                        principalTable: "avatars",
                        principalColumn: "avatar_address",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "reward_base_model_reward_policy_model",
                columns: table => new
                {
                    reward_policies_id = table.Column<int>(type: "integer", nullable: false),
                    rewards_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reward_base_model_reward_policy_model", x => new { x.reward_policies_id, x.rewards_id });
                    table.ForeignKey(
                        name: "fk_reward_base_model_reward_policy_model_reward_policies_rewar",
                        column: x => x.reward_policies_id,
                        principalTable: "reward_policies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_reward_base_model_reward_policy_model_rewards_rewards_id",
                        column: x => x.rewards_id,
                        principalTable: "rewards",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "claims",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    avatar_address = table.Column<string>(type: "text", nullable: false),
                    policy_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    tx_id = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_claims", x => x.id);
                    table.ForeignKey(
                        name: "fk_claims_avatars_avatar_address1",
                        column: x => x.avatar_address,
                        principalTable: "avatars",
                        principalColumn: "avatar_address",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_claims_reward_policies_policy_id",
                        column: x => x.policy_id,
                        principalTable: "reward_policies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_claims_transactions_tx_id",
                        column: x => x.tx_id,
                        principalTable: "transactions",
                        principalColumn: "tx_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "garages",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    claim_id = table.Column<int>(type: "integer", nullable: false),
                    reward_id = table.Column<int>(type: "integer", nullable: false),
                    count = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_garages", x => x.id);
                    table.ForeignKey(
                        name: "fk_garages_claims_claim_id",
                        column: x => x.claim_id,
                        principalTable: "claims",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_garages_rewards_reward_id",
                        column: x => x.reward_id,
                        principalTable: "rewards",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_claims_avatar_address",
                table: "claims",
                column: "avatar_address");

            migrationBuilder.CreateIndex(
                name: "ix_claims_policy_id",
                table: "claims",
                column: "policy_id");

            migrationBuilder.CreateIndex(
                name: "ix_claims_tx_id",
                table: "claims",
                column: "tx_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_garages_claim_id",
                table: "garages",
                column: "claim_id");

            migrationBuilder.CreateIndex(
                name: "ix_garages_reward_id",
                table: "garages",
                column: "reward_id");

            migrationBuilder.CreateIndex(
                name: "ix_reward_base_model_reward_policy_model_rewards_id",
                table: "reward_base_model_reward_policy_model",
                column: "rewards_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "garages");

            migrationBuilder.DropTable(
                name: "reward_base_model_reward_policy_model");

            migrationBuilder.DropTable(
                name: "claims");

            migrationBuilder.DropTable(
                name: "rewards");

            migrationBuilder.DropTable(
                name: "reward_policies");

            migrationBuilder.DropTable(
                name: "transactions");

            migrationBuilder.DropTable(
                name: "avatars");
        }
    }
}
