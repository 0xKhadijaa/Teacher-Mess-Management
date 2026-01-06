using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mess_Management_System.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BillId",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "BillIssueId",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "ContactMessageId",
                table: "Notifications");

            migrationBuilder.AddColumn<string>(
                name: "Link",
                table: "Notifications",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
