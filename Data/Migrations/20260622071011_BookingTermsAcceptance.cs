using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeMaids.Data.Migrations
{
    /// <inheritdoc />
    public partial class BookingTermsAcceptance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "TermsAcceptedAt",
                table: "Bookings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TermsVersion",
                table: "Bookings",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TermsAcceptedAt",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "TermsVersion",
                table: "Bookings");
        }
    }
}
