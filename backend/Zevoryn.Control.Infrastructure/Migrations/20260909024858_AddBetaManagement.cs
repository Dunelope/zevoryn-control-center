using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zevoryn.Control.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBetaManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "beta_campaigns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    MaxInvitations = table.Column<int>(type: "integer", nullable: false),
                    StartsAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EndsAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_beta_campaigns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_beta_campaigns_products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "beta_invitations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BetaCampaignId = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ExternalReference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    InvitedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AcceptedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_beta_invitations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_beta_invitations_beta_campaigns_BetaCampaignId",
                        column: x => x.BetaCampaignId,
                        principalTable: "beta_campaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_beta_campaigns_ProductId",
                table: "beta_campaigns",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_beta_campaigns_Status",
                table: "beta_campaigns",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_beta_invitations_BetaCampaignId",
                table: "beta_invitations",
                column: "BetaCampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_beta_invitations_BetaCampaignId_Email",
                table: "beta_invitations",
                columns: new[] { "BetaCampaignId", "Email" },
                unique: true,
                filter: "\"Status\" NOT IN ('Revoked', 'Expired')");

            migrationBuilder.CreateIndex(
                name: "IX_beta_invitations_Status",
                table: "beta_invitations",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "beta_invitations");

            migrationBuilder.DropTable(
                name: "beta_campaigns");
        }
    }
}
