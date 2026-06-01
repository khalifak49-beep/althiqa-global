using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeMaids.Data.Migrations
{
    /// <inheritdoc />
    public partial class MonthlyBookingsAndCouponFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ContractEndDate",
                table: "Bookings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MonthlyPlan",
                table: "Bookings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MonthlyVisits",
                table: "Bookings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "Bookings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "Coupons",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Description", "MaxDiscountAmount", "MinOrderAmount" },
                values: new object[] { "خصم 20% للعملاء الجدد (حد أقصى 5 ر.ع.)", 5m, 5m });

            migrationBuilder.UpdateData(
                table: "Coupons",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Code", "Description", "DiscountValue", "MinOrderAmount" },
                values: new object[] { "SAVE2", "خصم 2 ر.ع. على الحجوزات أكثر من 8 ر.ع.", 2m, 8m });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContractEndDate",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "MonthlyPlan",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "MonthlyVisits",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Bookings");

            migrationBuilder.UpdateData(
                table: "Coupons",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Description", "MaxDiscountAmount", "MinOrderAmount" },
                values: new object[] { "خصم 20% للعملاء الجدد", 100m, 50m });

            migrationBuilder.UpdateData(
                table: "Coupons",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Code", "Description", "DiscountValue", "MinOrderAmount" },
                values: new object[] { "SAVE50", "خصم 50 ريال على الحجوزات أكثر من 200", 50m, 200m });
        }
    }
}
