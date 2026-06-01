using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeMaids.Data.Migrations
{
    /// <inheritdoc />
    public partial class OmaniServicesAndRates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Services",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.UpdateData(
                table: "Services",
                keyColumn: "Id",
                keyValue: 1,
                column: "BasePrice",
                value: 3.000m);

            migrationBuilder.UpdateData(
                table: "Services",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "BasePrice", "Description", "IconClass", "Name" },
                values: new object[] { 3.000m, "تنظيف داخلي تفصيلي للأرضيات والمطبخ والحمامات", "bi-house-door", "تنظيف داخلي" });

            migrationBuilder.UpdateData(
                table: "Services",
                keyColumn: "Id",
                keyValue: 3,
                column: "BasePrice",
                value: 3.000m);

            migrationBuilder.UpdateData(
                table: "Services",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "BasePrice", "Description", "IconClass", "Name" },
                values: new object[] { 3.500m, "تنظيف الواجهات والأسطح والحديقة", "bi-tree", "تنظيف خارجي" });

            migrationBuilder.UpdateData(
                table: "Workers",
                keyColumn: "Id",
                keyValue: 1,
                column: "HourlyRate",
                value: 3.500m);

            migrationBuilder.UpdateData(
                table: "Workers",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Bio", "HourlyRate" },
                values: new object[] { "متخصصة في التنظيف الداخلي وغسيل الستائر.", 4.000m });

            migrationBuilder.UpdateData(
                table: "Workers",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Bio", "HourlyRate" },
                values: new object[] { "خبرة في تنظيف الواجهات والأسطح.", 3.800m });

            migrationBuilder.UpdateData(
                table: "Workers",
                keyColumn: "Id",
                keyValue: 4,
                column: "HourlyRate",
                value: 3.000m);

            migrationBuilder.UpdateData(
                table: "Workers",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "Bio", "HourlyRate", "ServiceId" },
                values: new object[] { "تنظيف عام واهتمام بالتفاصيل.", 3.200m, 1 });

            migrationBuilder.UpdateData(
                table: "Workers",
                keyColumn: "Id",
                keyValue: 6,
                column: "HourlyRate",
                value: 4.500m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Services",
                keyColumn: "Id",
                keyValue: 1,
                column: "BasePrice",
                value: 25m);

            migrationBuilder.UpdateData(
                table: "Services",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "BasePrice", "Description", "IconClass", "Name" },
                values: new object[] { 40m, "تنظيف تفصيلي شامل للأرضيات والمطبخ والحمامات", "bi-droplet-half", "تنظيف عميق" });

            migrationBuilder.UpdateData(
                table: "Services",
                keyColumn: "Id",
                keyValue: 3,
                column: "BasePrice",
                value: 30m);

            migrationBuilder.UpdateData(
                table: "Services",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "BasePrice", "Description", "IconClass", "Name" },
                values: new object[] { 35m, "إعداد وجبات منزلية", "bi-cup-hot", "طهي ومطبخ" });

            migrationBuilder.InsertData(
                table: "Services",
                columns: new[] { "Id", "BasePrice", "Description", "IconClass", "IsActive", "Name" },
                values: new object[] { 5, 45m, "جليسة أطفال ومرافقة", "bi-emoji-smile", true, "رعاية أطفال" });

            migrationBuilder.UpdateData(
                table: "Workers",
                keyColumn: "Id",
                keyValue: 1,
                column: "HourlyRate",
                value: 35m);

            migrationBuilder.UpdateData(
                table: "Workers",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Bio", "HourlyRate" },
                values: new object[] { "متخصصة في التنظيف العميق وغسيل الستائر.", 40m });

            migrationBuilder.UpdateData(
                table: "Workers",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Bio", "HourlyRate" },
                values: new object[] { "خبرة في الطهي ورعاية المنزل.", 38m });

            migrationBuilder.UpdateData(
                table: "Workers",
                keyColumn: "Id",
                keyValue: 4,
                column: "HourlyRate",
                value: 30m);

            migrationBuilder.UpdateData(
                table: "Workers",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "Bio", "HourlyRate", "ServiceId" },
                values: new object[] { "تجيد رعاية الأطفال وكبار السن.", 42m, 5 });

            migrationBuilder.UpdateData(
                table: "Workers",
                keyColumn: "Id",
                keyValue: 6,
                column: "HourlyRate",
                value: 45m);
        }
    }
}
