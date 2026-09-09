using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zevoryn.Control.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCleanersFlowBetaIntegrationGenerated : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(name: "EnvironmentId", table: "beta_campaigns", type: "uuid", nullable: true);
            migrationBuilder.AddColumn<string>(name: "ErrorCode", table: "beta_invitations", type: "character varying(80)", maxLength: 80, nullable: true);
            migrationBuilder.AddColumn<string>(name: "ErrorMessage", table: "beta_invitations", type: "character varying(500)", maxLength: 500, nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "EnvironmentId", table: "beta_campaigns");
            migrationBuilder.DropColumn(name: "ErrorCode", table: "beta_invitations");
            migrationBuilder.DropColumn(name: "ErrorMessage", table: "beta_invitations");
        }
    }
}
