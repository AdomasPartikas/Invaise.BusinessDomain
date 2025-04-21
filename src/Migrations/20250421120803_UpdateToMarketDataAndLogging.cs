using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Invaise.BusinessDomain.API.Migrations
{
    /// <inheritdoc />
    public partial class UpdateToMarketDataAndLogging : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LogEntries");

            migrationBuilder.DropTable(
                name: "MarketData");

            migrationBuilder.DropTable(
                name: "MarketDataDaily");

            migrationBuilder.CreateTable(
                name: "HistoricalMarketData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Symbol = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Date = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Open = table.Column<decimal>(type: "decimal(65,30)", nullable: true),
                    High = table.Column<decimal>(type: "decimal(65,30)", nullable: true),
                    Low = table.Column<decimal>(type: "decimal(65,30)", nullable: true),
                    Close = table.Column<decimal>(type: "decimal(65,30)", nullable: true),
                    Volume = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistoricalMarketData", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "IntradayMarketData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Symbol = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Timestamp = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Open = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    High = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Low = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Current = table.Column<decimal>(type: "decimal(65,30)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntradayMarketData", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "LogEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TimeStamp = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Level = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Message = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MessageTemplate = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Exception = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CorrelationId = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ProcessId = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ProcessName = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ClientIp = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Properties = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogEvents", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_HistoricalMarketData_Symbol_Date",
                table: "HistoricalMarketData",
                columns: new[] { "Symbol", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IntradayMarketData_Symbol_Timestamp",
                table: "IntradayMarketData",
                columns: new[] { "Symbol", "Timestamp" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HistoricalMarketData");

            migrationBuilder.DropTable(
                name: "IntradayMarketData");

            migrationBuilder.DropTable(
                name: "LogEvents");

            migrationBuilder.CreateTable(
                name: "LogEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<long>(type: "bigint", nullable: true),
                    Level = table.Column<int>(type: "int", nullable: false),
                    Message = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Service = table.Column<int>(type: "int", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LogEntries_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "MarketData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Close = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    High = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Low = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Open = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Symbol = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Volume = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketData", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "MarketDataDaily",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Current = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    High = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Low = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Open = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Symbol = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Timestamp = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketDataDaily", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_LogEntries_UserId",
                table: "LogEntries",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_MarketData_Symbol_Date",
                table: "MarketData",
                columns: new[] { "Symbol", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MarketDataDaily_Symbol_Timestamp",
                table: "MarketDataDaily",
                columns: new[] { "Symbol", "Timestamp" },
                unique: true);
        }
    }
}
