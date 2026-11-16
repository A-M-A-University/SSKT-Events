using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SSKT_Events.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPayMongoCheckoutSessionId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PayMongoCheckoutSessionId",
                table: "EventPayments",
                type: "TEXT",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PayMongoCheckoutSessionId",
                table: "EventPayments");
        }
    }
}
