using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Invaise.BusinessDomain.API.Migrations
{
    /// <inheritdoc />
    public partial class AddPortfolioPerformances : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PortfolioPerformances",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PortfolioId = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Date = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    TotalValue = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    DailyChangePercent = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    WeeklyChangePercent = table.Column<decimal>(type: "decimal(65,30)", nullable: true),
                    MonthlyChangePercent = table.Column<decimal>(type: "decimal(65,30)", nullable: true),
                    YearlyChangePercent = table.Column<decimal>(type: "decimal(65,30)", nullable: true),
                    TotalStocks = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PortfolioPerformances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PortfolioPerformances_Portfolios_PortfolioId",
                        column: x => x.PortfolioId,
                        principalTable: "Portfolios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_PortfolioPerformances_PortfolioId_Date",
                table: "PortfolioPerformances",
                columns: new[] { "PortfolioId", "Date" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PortfolioPerformances");
        }
    }
}
