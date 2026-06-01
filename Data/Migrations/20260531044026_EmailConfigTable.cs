using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeMaids.Data.Migrations
{
    /// <inheritdoc />
    public partial class EmailConfigTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmailConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Host = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Port = table.Column<int>(type: "int", nullable: false),
                    EnableSsl = table.Column<bool>(type: "bit", nullable: false),
                    Username = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    AppPassword = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    FromEmail = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    FromName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ShowOtpInDev = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailConfigs", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "EmailConfigs",
                columns: new[] { "Id", "AppPassword", "EnableSsl", "FromEmail", "FromName", "Host", "IsActive", "Port", "ShowOtpInDev", "UpdatedAt", "Username" },
                values: new object[] { 1, "", true, "althiqaglobalom@gmail.com", "الثقة العالمية لخدمات التنظيف", "smtp.gmail.com", true, 587, true, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "althiqaglobalom@gmail.com" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailConfigs");
        }
    }
}
