# الثقة العالمية لخدمات التنظيف — Al Thiqa Global Cleaning Services

نظام ويب احترافي لحجز عاملات التنظيف بنظام **الساعات** و **العقود الشهرية** في سلطنة عُمان.

## التقنيات

- ASP.NET Core MVC 9.0
- Entity Framework Core + SQL Server
- ASP.NET Identity (دخول بكلمة سر + OTP بريد + OTP جوال)
- Bootstrap 5 RTL + AOS animations
- Leaflet maps (OpenStreetMap)
- Thawani Pay integration (بوابة دفع عُمانية)
- PWA installable
- QuestPDF for invoices
- Serilog logging

## المميزات

### للعميل
- 🏠 تصفّح العاملات بالفلترة (الخدمة، الجنسية، السعر، التقييم)
- ⏱️ حجز بالساعات (الحد الأدنى ساعتان)
- 📅 عقد شهري ثابت (أسبوعي / مرتان أسبوعياً / 3 مرات / يومي)
- 🗺️ تحديد الموقع على الخريطة + موقعي الحالي
- 💳 دفع آمن عبر Thawani Pay
- 🎟️ كوبونات خصم (WELCOME20, SAVE2)
- 📧 تسجيل/دخول بـ OTP عبر البريد (Gmail SMTP)
- 📱 تسجيل/دخول بـ OTP عبر الجوال (WhatsApp - 3 modes)
- ❤️ المفضلة + التقييمات
- 🧾 فواتير PDF
- 📲 PWA installable

### للأدمن
- 📊 لوحة تحكم بإحصائيات + رسوم بيانية Chart.js
- 👩 إدارة العاملات (إضافة، تعديل، حذف، رفع صور)
- 📅 جدول دوام العاملة (لكل يوم من الأسبوع)
- 📋 إدارة الحجوزات + تغيير الحالة
- 💰 إدارة الكوبونات والعروض
- 👥 إدارة المستخدمين
- 📈 تقارير بمدى زمني
- ⚙️ إعدادات Thawani (قابل للتعديل من القاعدة)
- 📧 إعدادات Gmail SMTP (قابل للتعديل من القاعدة)
- 📱 إعدادات WhatsApp OTP
- 🖼️ رفع شعار البزنس

### REST API للموبايل
- `/api/auth/{register,login}`
- `/api/workers`
- `/api/bookings` (mine, quote, availability, create, cancel)
- `/swagger` للتوثيق

## التشغيل المحلي

```powershell
# 1. SQL Server (LocalDB أو SQLEXPRESS)
# تأكد من أن خادم SQL Server يعمل

# 2. تثبيت الحزم
dotnet restore

# 3. تطبيق Migrations
dotnet ef database update

# 4. التشغيل
dotnet run
# الموقع على http://localhost:5050
```

## بيانات الدخول الافتراضية

- **الأدمن**: `admin@homemaids.local` / `Admin@123456`

## ملف الإعدادات

`appsettings.json`:
- `ConnectionStrings:DefaultConnection` — connection string لـ SQL Server
- `AdminSeed` — بيانات أول أدمن
- `BookingSettings` — TaxPercent (5% عمان), MinHours (2), MaxHours (12)
- `WhatsApp` — إعدادات OTP (Mode: log/callmebot/cloud)
- `Email` — fallback SMTP إذا لم تُهيّأ من القاعدة

## النشر

- يدعم Docker وقابل للرفع على أي مزود يدعم ASP.NET Core + SQL Server
- مزودون مقترحون: MonsterASP.NET, Somee.com, Azure App Service, Render

## License

Private — © Al Thiqa Global Cleaning Services
