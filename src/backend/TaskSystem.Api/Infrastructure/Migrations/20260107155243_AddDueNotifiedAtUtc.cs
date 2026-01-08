using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskSystem.Api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDueNotifiedAtUtc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "Users",
                type: "TEXT",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "TEXT",
                oldDefaultValueSql: "sysutcdatetime()");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "Tasks",
                type: "TEXT",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "TEXT",
                oldDefaultValueSql: "sysutcdatetime()");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "Tasks",
                type: "TEXT",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "TEXT",
                oldDefaultValueSql: "sysutcdatetime()");

            migrationBuilder.AddColumn<DateTime>(
                name: "DueNotifiedAtUtc",
                table: "Tasks",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_DueNotifiedAtUtc_DueDateUtc",
                table: "Tasks",
                columns: new[] { "DueNotifiedAtUtc", "DueDateUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tasks_DueNotifiedAtUtc_DueDateUtc",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "DueNotifiedAtUtc",
                table: "Tasks");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "Users",
                type: "TEXT",
                nullable: false,
                defaultValueSql: "sysutcdatetime()",
                oldClrType: typeof(DateTime),
                oldType: "TEXT",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "Tasks",
                type: "TEXT",
                nullable: false,
                defaultValueSql: "sysutcdatetime()",
                oldClrType: typeof(DateTime),
                oldType: "TEXT",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "Tasks",
                type: "TEXT",
                nullable: false,
                defaultValueSql: "sysutcdatetime()",
                oldClrType: typeof(DateTime),
                oldType: "TEXT",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");
        }
    }
}
