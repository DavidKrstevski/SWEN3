using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Paperless_API.Migrations
{
    /// <inheritdoc />
    public partial class ResetSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "FileName",
                table: "Documents",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "ChatHistory",
                table: "Documents",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RiskColor",
                table: "Documents",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Summary",
                table: "Documents",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChatHistory",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "RiskColor",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "Summary",
                table: "Documents");

            migrationBuilder.AlterColumn<string>(
                name: "FileName",
                table: "Documents",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);
        }
    }
}
