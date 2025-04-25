using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Invaise.BusinessDomain.API.Migrations
{
    /// <inheritdoc />
    public partial class AddDirectionToHeat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Direction",
                table: "Heats",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Direction",
                table: "Heats");
        }
    }
}
