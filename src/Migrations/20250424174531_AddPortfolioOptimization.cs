using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Invaise.BusinessDomain.API.Migrations
{
    /// <inheritdoc />
    public partial class AddPortfolioOptimization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PortfolioOptimizations",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UserId = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PortfolioId = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Timestamp = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Explanation = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Confidence = table.Column<double>(type: "double", nullable: false),
                    RiskTolerance = table.Column<double>(type: "double", nullable: true),
                    IsApplied = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    AppliedDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ModelVersion = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PortfolioOptimizations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PortfolioOptimizations_Portfolios_PortfolioId",
                        column: x => x.PortfolioId,
                        principalTable: "Portfolios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PortfolioOptimizationRecommendations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    OptimizationId = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Symbol = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Action = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CurrentQuantity = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    TargetQuantity = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    CurrentWeight = table.Column<double>(type: "double", nullable: false),
                    TargetWeight = table.Column<double>(type: "double", nullable: false),
                    Explanation = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PortfolioOptimizationRecommendations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PortfolioOptimizationRecommendations_PortfolioOptimizations_~",
                        column: x => x.OptimizationId,
                        principalTable: "PortfolioOptimizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_PortfolioOptimizationRecommendations_OptimizationId",
                table: "PortfolioOptimizationRecommendations",
                column: "OptimizationId");

            migrationBuilder.CreateIndex(
                name: "IX_PortfolioOptimizations_PortfolioId",
                table: "PortfolioOptimizations",
                column: "PortfolioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PortfolioOptimizationRecommendations");

            migrationBuilder.DropTable(
                name: "PortfolioOptimizations");
        }
    }
}
