using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketingSystem.DAL.EF.Migrations
{
    /// <inheritdoc />
    public partial class CascadeOrderItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderItems_EventSeats_EventSeatId",
                table: "OrderItems");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderItems_EventSeats_EventSeatId",
                table: "OrderItems",
                column: "EventSeatId",
                principalTable: "EventSeats",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderItems_EventSeats_EventSeatId",
                table: "OrderItems");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderItems_EventSeats_EventSeatId",
                table: "OrderItems",
                column: "EventSeatId",
                principalTable: "EventSeats",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
