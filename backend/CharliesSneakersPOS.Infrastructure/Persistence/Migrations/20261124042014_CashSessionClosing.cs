using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CharliesSneakersPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CashSessionClosing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ClosedAt",
                table: "CashSessions",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CountedAmount",
                table: "CashSessions",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ExpectedAmount",
                table: "CashSessions",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "CashSessions",
                type: "rowversion",
                rowVersion: true,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClosedAt",
                table: "CashSessions");

            migrationBuilder.DropColumn(
                name: "CountedAmount",
                table: "CashSessions");

            migrationBuilder.DropColumn(
                name: "ExpectedAmount",
                table: "CashSessions");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "CashSessions");
        }
    }
}
