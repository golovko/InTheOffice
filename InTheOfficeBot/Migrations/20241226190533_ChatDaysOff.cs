using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InTheOfficeBot.Migrations
{
    /// <inheritdoc />
    public partial class ChatDaysOff : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DaysOff",
                table: "Chats",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DaysOff",
                table: "Chats");
        }
    }
}
