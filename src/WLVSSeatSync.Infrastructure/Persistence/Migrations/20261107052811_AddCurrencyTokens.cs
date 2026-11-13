using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WLVSSeatSync.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCurrencyTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "Orders",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "EventSeats",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Version",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "EventSeats");
        }
    }
}
