using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Invaise.BusinessDomain.API.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceMetrics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "ExpectedReturn",
                table: "PortfolioOptimizations",
                type: "double",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "MeanReturn",
                table: "PortfolioOptimizations",
                type: "double",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "SharpeRatio",
                table: "PortfolioOptimizations",
                type: "double",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Variance",
                table: "PortfolioOptimizations",
                type: "double",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExpectedReturn",
                table: "PortfolioOptimizations");

            migrationBuilder.DropColumn(
                name: "MeanReturn",
                table: "PortfolioOptimizations");

            migrationBuilder.DropColumn(
                name: "SharpeRatio",
                table: "PortfolioOptimizations");

            migrationBuilder.DropColumn(
                name: "Variance",
                table: "PortfolioOptimizations");
        }
    }
}
