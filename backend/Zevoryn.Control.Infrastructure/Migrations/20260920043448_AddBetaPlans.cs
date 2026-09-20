using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zevoryn.Control.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBetaPlans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BetaPlan",
                table: "beta_invitations",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BetaPlan",
                table: "beta_campaigns",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BetaPlan",
                table: "beta_invitations");

            migrationBuilder.DropColumn(
                name: "BetaPlan",
                table: "beta_campaigns");
        }
    }
}
