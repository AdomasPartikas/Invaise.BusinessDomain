using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Invaise.BusinessDomain.API.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectedMetricsToOptimization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "ProjectedExpectedReturn",
                table: "PortfolioOptimizations",
                type: "double",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "ProjectedMeanReturn",
                table: "PortfolioOptimizations",
                type: "double",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "ProjectedSharpeRatio",
                table: "PortfolioOptimizations",
                type: "double",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "ProjectedVariance",
                table: "PortfolioOptimizations",
                type: "double",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProjectedExpectedReturn",
                table: "PortfolioOptimizations");

            migrationBuilder.DropColumn(
                name: "ProjectedMeanReturn",
                table: "PortfolioOptimizations");

            migrationBuilder.DropColumn(
                name: "ProjectedSharpeRatio",
                table: "PortfolioOptimizations");

            migrationBuilder.DropColumn(
                name: "ProjectedVariance",
                table: "PortfolioOptimizations");
        }
    }
}
