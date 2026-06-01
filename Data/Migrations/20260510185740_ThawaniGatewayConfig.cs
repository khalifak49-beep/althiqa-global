using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeMaids.Data.Migrations
{
    /// <inheritdoc />
    public partial class ThawaniGatewayConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PaymentGatewayConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Provider = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    ApiBaseUrl = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    CheckoutBaseUrl = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    SecretKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PublishableKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SuccessUrl = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CancelUrl = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsLive = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentGatewayConfigs", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "PaymentGatewayConfigs",
                columns: new[] { "Id", "ApiBaseUrl", "CancelUrl", "CheckoutBaseUrl", "DisplayName", "IsActive", "IsLive", "Provider", "PublishableKey", "SecretKey", "SuccessUrl", "UpdatedAt" },
                values: new object[] { 1, "https://uatcheckout.thawani.om/api/v1", "/Payments/ThawaniCancel", "https://uatcheckout.thawani.om/pay/", "Thawani Pay", true, false, "Thawani", "HGvTMLDssJghr9tlN9gr4DVYt0qyBy", "rRQ26GcsZzoEhbrP2HZvLYDbn9C9et", "/Payments/ThawaniSuccess", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentGatewayConfigs_Provider",
                table: "PaymentGatewayConfigs",
                column: "Provider",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PaymentGatewayConfigs");
        }
    }
}
