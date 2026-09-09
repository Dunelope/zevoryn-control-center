using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zevoryn.Control.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCloudflareAccessServiceAuth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(name: "AccessClientIdSecretReference", table: "product_connections", type: "character varying(200)", maxLength: 200, nullable: true);
            migrationBuilder.AddColumn<string>(name: "AccessClientSecretSecretReference", table: "product_connections", type: "character varying(200)", maxLength: 200, nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "AccessClientIdSecretReference", table: "product_connections");
            migrationBuilder.DropColumn(name: "AccessClientSecretSecretReference", table: "product_connections");
        }
    }
}
