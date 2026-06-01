using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HomeMaids.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AvatarUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DefaultAddress = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LoyaltyPoints = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Coupons",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DiscountType = table.Column<int>(type: "int", nullable: false),
                    DiscountValue = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    MinOrderAmount = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    MaxDiscountAmount = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    ValidFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValidTo = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsageLimit = table.Column<int>(type: "int", nullable: true),
                    UsedCount = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Coupons", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Offers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DiscountType = table.Column<int>(type: "int", nullable: false),
                    DiscountValue = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    ValidFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValidTo = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Offers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Services",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IconClass = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    BasePrice = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Services", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    ActionUrl = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserAddresses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AddressLine = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    City = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PostalCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAddresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserAddresses_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Workers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FullName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Age = table.Column<int>(type: "int", nullable: false),
                    Nationality = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    YearsOfExperience = table.Column<int>(type: "int", nullable: false),
                    Languages = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Bio = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    PhotoUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    HourlyRate = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    AverageRating = table.Column<decimal>(type: "decimal(3,2)", nullable: false),
                    TotalBookings = table.Column<int>(type: "int", nullable: false),
                    Availability = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ServiceId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Workers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Workers_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Bookings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BookingNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CustomerId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    WorkerId = table.Column<int>(type: "int", nullable: false),
                    ServiceId = table.Column<int>(type: "int", nullable: true),
                    BookingDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    Hours = table.Column<int>(type: "int", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SubTotal = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    CouponId = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CancelledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancellationReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bookings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bookings_AspNetUsers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Bookings_Coupons_CouponId",
                        column: x => x.CouponId,
                        principalTable: "Coupons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Bookings_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Bookings_Workers_WorkerId",
                        column: x => x.WorkerId,
                        principalTable: "Workers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Favorites",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    WorkerId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Favorites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Favorites_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Favorites_Workers_WorkerId",
                        column: x => x.WorkerId,
                        principalTable: "Workers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkerSchedules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkerId = table.Column<int>(type: "int", nullable: false),
                    DayOfWeek = table.Column<int>(type: "int", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    IsAvailable = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkerSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkerSchedules_Workers_WorkerId",
                        column: x => x.WorkerId,
                        principalTable: "Workers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TransactionRef = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    BookingId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Method = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CardLast4 = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CardHolderName = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    GatewayResponse = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Payments_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Reviews",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BookingId = table.Column<int>(type: "int", nullable: false),
                    WorkerId = table.Column<int>(type: "int", nullable: false),
                    CustomerId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reviews_AspNetUsers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reviews_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Reviews_Workers_WorkerId",
                        column: x => x.WorkerId,
                        principalTable: "Workers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Coupons",
                columns: new[] { "Id", "Code", "Description", "DiscountType", "DiscountValue", "IsActive", "MaxDiscountAmount", "MinOrderAmount", "UsageLimit", "UsedCount", "ValidFrom", "ValidTo" },
                values: new object[,]
                {
                    { 1, "WELCOME20", "خصم 20% للعملاء الجدد", 0, 20m, true, 100m, 50m, 1000, 0, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 12, 31, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2, "SAVE50", "خصم 50 ريال على الحجوزات أكثر من 200", 1, 50m, true, null, 200m, 500, 0, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 12, 31, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "Offers",
                columns: new[] { "Id", "CreatedAt", "Description", "DiscountType", "DiscountValue", "ImageUrl", "IsActive", "Title", "ValidFrom", "ValidTo" },
                values: new object[,]
                {
                    { 1, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "خصم 20% على أول حجز للعملاء الجدد", 0, 20m, "/images/offers/welcome.jpg", true, "خصم الترحيب 20%", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 12, 31, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "خصم خاص بمناسبة شهر رمضان المبارك", 0, 15m, "/images/offers/ramadan.jpg", true, "عرض رمضان", new DateTime(2026, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 30, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "Services",
                columns: new[] { "Id", "BasePrice", "Description", "IconClass", "IsActive", "Name" },
                values: new object[,]
                {
                    { 1, 25m, "تنظيف منزلي شامل لكل الغرف", "bi-house-heart", true, "تنظيف عام" },
                    { 2, 40m, "تنظيف تفصيلي شامل للأرضيات والمطبخ والحمامات", "bi-droplet-half", true, "تنظيف عميق" },
                    { 3, 30m, "خدمات غسيل وكي مع التوصيل", "bi-tsunami", true, "غسيل وكي ملابس" },
                    { 4, 35m, "إعداد وجبات منزلية", "bi-cup-hot", true, "طهي ومطبخ" },
                    { 5, 45m, "جليسة أطفال ومرافقة", "bi-emoji-smile", true, "رعاية أطفال" }
                });

            migrationBuilder.InsertData(
                table: "Workers",
                columns: new[] { "Id", "Age", "Availability", "AverageRating", "Bio", "CreatedAt", "FullName", "HourlyRate", "IsActive", "Languages", "Nationality", "PhotoUrl", "ServiceId", "TotalBookings", "YearsOfExperience" },
                values: new object[,]
                {
                    { 1, 32, 0, 4.8m, "خبرة طويلة في تنظيف المنازل والاهتمام بالتفاصيل.", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "فاطمة عبدالله", 35m, true, "العربية، الإنجليزية", "فلبينية", "/images/workers/w1.jpg", 1, 124, 8 },
                    { 2, 29, 0, 4.7m, "متخصصة في التنظيف العميق وغسيل الستائر.", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "ماريا سانتوس", 40m, true, "الإنجليزية", "فلبينية", "/images/workers/w2.jpg", 2, 89, 5 },
                    { 3, 34, 0, 4.9m, "خبرة في الطهي ورعاية المنزل.", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "أمينة كانتي", 38m, true, "الإنجليزية، السواحلية", "كينية", "/images/workers/w3.jpg", 4, 156, 7 },
                    { 4, 27, 1, 4.6m, "متخصصة في غسيل وكي الملابس.", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "ساندرا ميندوزا", 30m, true, "الإندونيسية، الإنجليزية", "إندونيسية", "/images/workers/w4.jpg", 3, 67, 4 },
                    { 5, 31, 0, 4.85m, "تجيد رعاية الأطفال وكبار السن.", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "ليلى نصور", 42m, true, "العربية، الأمهرية", "إثيوبية", "/images/workers/w5.jpg", 5, 98, 6 },
                    { 6, 35, 0, 4.95m, "خبرة عشر سنوات في خدمة المنازل.", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "كلارا أوغوستو", 45m, true, "الإنجليزية، التاغالوغ", "فلبينية", "/images/workers/w6.jpg", 1, 210, 10 }
                });

            migrationBuilder.InsertData(
                table: "WorkerSchedules",
                columns: new[] { "Id", "DayOfWeek", "EndTime", "IsAvailable", "StartTime", "WorkerId" },
                values: new object[,]
                {
                    { 1, 0, new TimeSpan(0, 20, 0, 0, 0), true, new TimeSpan(0, 8, 0, 0, 0), 1 },
                    { 2, 1, new TimeSpan(0, 20, 0, 0, 0), true, new TimeSpan(0, 8, 0, 0, 0), 1 },
                    { 3, 2, new TimeSpan(0, 20, 0, 0, 0), true, new TimeSpan(0, 8, 0, 0, 0), 1 },
                    { 4, 3, new TimeSpan(0, 20, 0, 0, 0), true, new TimeSpan(0, 8, 0, 0, 0), 1 },
                    { 5, 4, new TimeSpan(0, 20, 0, 0, 0), true, new TimeSpan(0, 8, 0, 0, 0), 1 },
                    { 6, 5, new TimeSpan(0, 20, 0, 0, 0), false, new TimeSpan(0, 8, 0, 0, 0), 1 },
                    { 7, 6, new TimeSpan(0, 20, 0, 0, 0), true, new TimeSpan(0, 8, 0, 0, 0), 1 },
                    { 8, 0, new TimeSpan(0, 20, 0, 0, 0), true, new TimeSpan(0, 8, 0, 0, 0), 2 },
                    { 9, 1, new TimeSpan(0, 20, 0, 0, 0), true, new TimeSpan(0, 8, 0, 0, 0), 2 },
                    { 10, 2, new TimeSpan(0, 20, 0, 0, 0), true, new TimeSpan(0, 8, 0, 0, 0), 2 },
                    { 11, 3, new TimeSpan(0, 20, 0, 0, 0), true, new TimeSpan(0, 8, 0, 0, 0), 2 },
                    { 12, 4, new TimeSpan(0, 20, 0, 0, 0), true, new TimeSpan(0, 8, 0, 0, 0), 2 },
                    { 13, 5, new TimeSpan(0, 20, 0, 0, 0), false, new TimeSpan(0, 8, 0, 0, 0), 2 },
                    { 14, 6, new TimeSpan(0, 20, 0, 0, 0), true, new TimeSpan(0, 8, 0, 0, 0), 2 },
                    { 15, 0, new TimeSpan(0, 20, 0, 0, 0), true, new TimeSpan(0, 8, 0, 0, 0), 3 },
                    { 16, 1, new TimeSpan(0, 20, 0, 0, 0), true, new TimeSpan(0, 8, 0, 0, 0), 3 },
                    { 17, 2, new TimeSpan(0, 20, 0, 0, 0), true, new TimeSpan(0, 8, 0, 0, 0), 3 },
                    { 18, 3, new TimeSpan(0, 20, 0, 0, 0), true, new TimeSpan(0, 8, 0, 0, 0), 3 },
                    { 19, 4, new TimeSpan(0, 20, 0, 0, 0), true, new TimeSpan(0, 8, 0, 0, 0), 3 },
                    { 20, 5, new TimeSpan(0, 20, 0, 0, 0), false, new TimeSpan(0, 8, 0, 0, 0), 3 },
                    { 21, 6, new TimeSpan(0, 20, 0, 0, 0), true, new TimeSpan(0, 8, 0, 0, 0), 3 },
                    { 22, 0, new TimeSpan(0, 20, 0, 0, 0), true, new TimeSpan(0, 8, 0, 0, 0), 4 },
                    { 23, 1, new TimeSpan(0, 20, 0, 0, 0), true, new TimeSpan(0, 8, 0, 0, 0), 4 },
                    { 24, 2, new TimeSpan(0, 20, 0, 0, 0), true, new TimeSpan(0, 8, 0, 0, 0), 4 },
                    { 25, 3, new TimeSpan(0, 20, 0, 0, 0), true, new TimeSpan(0, 8, 0, 0, 0), 4 },
                    { 26, 4, new TimeSpan(0, 20, 0, 0, 0), true, new TimeSpan(0, 8, 0, 0, 0), 4 },
                    { 27, 5, new TimeSpan(0, 20, 0, 0, 0), false, new TimeSpan(0, 8, 0, 0, 0), 4 },
                    { 28, 6, new TimeSpan(0, 20, 0, 0, 0), true, new TimeSpan(0, 8, 0, 0, 0), 4 },
                    { 29, 0, new TimeSpan(0, 20, 0, 0, 0), true, new TimeSpan(0, 8, 0, 0, 0), 5 },
                    { 30, 1, new TimeSpan(0, 20, 0, 0, 0), true, new TimeSpan(0, 8, 0, 0, 0), 5 },
                    { 31, 2, new TimeSpan(0, 20, 0, 0, 0), true, new TimeSpan(0, 8, 0, 0, 0), 5 },
                    { 32, 3, new TimeSpan(0, 20, 0, 0, 0), true, new TimeSpan(0, 8, 0, 0, 0), 5 },
                    { 33, 4, new TimeSpan(0, 20, 0, 0, 0), true, new TimeSpan(0, 8, 0, 0, 0), 5 },
                    { 34, 5, new TimeSpan(0, 20, 0, 0, 0), false, new TimeSpan(0, 8, 0, 0, 0), 5 },
                    { 35, 6, new TimeSpan(0, 20, 0, 0, 0), true, new TimeSpan(0, 8, 0, 0, 0), 5 },
                    { 36, 0, new TimeSpan(0, 20, 0, 0, 0), true, new TimeSpan(0, 8, 0, 0, 0), 6 },
                    { 37, 1, new TimeSpan(0, 20, 0, 0, 0), true, new TimeSpan(0, 8, 0, 0, 0), 6 },
                    { 38, 2, new TimeSpan(0, 20, 0, 0, 0), true, new TimeSpan(0, 8, 0, 0, 0), 6 },
                    { 39, 3, new TimeSpan(0, 20, 0, 0, 0), true, new TimeSpan(0, 8, 0, 0, 0), 6 },
                    { 40, 4, new TimeSpan(0, 20, 0, 0, 0), true, new TimeSpan(0, 8, 0, 0, 0), 6 },
                    { 41, 5, new TimeSpan(0, 20, 0, 0, 0), false, new TimeSpan(0, 8, 0, 0, 0), 6 },
                    { 42, 6, new TimeSpan(0, 20, 0, 0, 0), true, new TimeSpan(0, 8, 0, 0, 0), 6 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_BookingNumber",
                table: "Bookings",
                column: "BookingNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_CouponId",
                table: "Bookings",
                column: "CouponId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_CustomerId",
                table: "Bookings",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_ServiceId",
                table: "Bookings",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_Status",
                table: "Bookings",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_WorkerId_BookingDate",
                table: "Bookings",
                columns: new[] { "WorkerId", "BookingDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Coupons_Code",
                table: "Coupons",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Favorites_UserId_WorkerId",
                table: "Favorites",
                columns: new[] { "UserId", "WorkerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Favorites_WorkerId",
                table: "Favorites",
                column: "WorkerId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_IsRead",
                table: "Notifications",
                columns: new[] { "UserId", "IsRead" });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_BookingId",
                table: "Payments",
                column: "BookingId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_TransactionRef",
                table: "Payments",
                column: "TransactionRef",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_BookingId",
                table: "Reviews",
                column: "BookingId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_CustomerId",
                table: "Reviews",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_WorkerId",
                table: "Reviews",
                column: "WorkerId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAddresses_UserId",
                table: "UserAddresses",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Workers_Availability",
                table: "Workers",
                column: "Availability");

            migrationBuilder.CreateIndex(
                name: "IX_Workers_FullName",
                table: "Workers",
                column: "FullName");

            migrationBuilder.CreateIndex(
                name: "IX_Workers_ServiceId",
                table: "Workers",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkerSchedules_WorkerId_DayOfWeek",
                table: "WorkerSchedules",
                columns: new[] { "WorkerId", "DayOfWeek" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "Favorites");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "Offers");

            migrationBuilder.DropTable(
                name: "Payments");

            migrationBuilder.DropTable(
                name: "Reviews");

            migrationBuilder.DropTable(
                name: "UserAddresses");

            migrationBuilder.DropTable(
                name: "WorkerSchedules");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "Bookings");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "Coupons");

            migrationBuilder.DropTable(
                name: "Workers");

            migrationBuilder.DropTable(
                name: "Services");
        }
    }
}
