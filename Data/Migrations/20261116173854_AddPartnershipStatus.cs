using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SSKT_Events.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPartnershipStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "OrganizerSuppliers",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "OrganizerSuppliers");
        }
    }
}
