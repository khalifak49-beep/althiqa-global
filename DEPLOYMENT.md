# دليل النشر على Render.com

نشر **الثقة العالمية لخدمات التنظيف** كموقع شغّال 24/7 على Render — مجاناً.

## ⚠️ ما تحتاجه قبل البدء

| المتطلب | كيفية الحصول عليه |
|---------|------------------|
| 📧 بريد إلكتروني | أي بريد عادي |
| 🐙 حساب GitHub | https://github.com/signup (مجاني) |
| 🚀 حساب Render | https://render.com/register (مجاني، يقبل تسجيل الدخول بـ GitHub) |
| 💳 بطاقة ائتمان | **غير مطلوبة للخطة المجانية** |

## ⚠️ قيود الخطة المجانية على Render

| القيد | التفاصيل | الحل عند الحاجة |
|------|----------|----------------|
| 🛏️ السيرفر ينام | بعد 15 دقيقة بدون زيارات، أول زائر بعدها ينتظر ~50 ثانية | استخدم UptimeRobot لإرسال ping كل 5 دقائق (مجاناً) |
| 📅 PostgreSQL ينتهي | بعد 90 يوم تُحذف القاعدة | أعد إنشاءها أو ترقَ لـ $7/شهر |
| ⚡ موارد محدودة | 512 MB RAM، 0.1 CPU | يكفي للبدء؛ ترقَ عند نمو الموقع |

---

## 📋 الخطوات

### الخطوة 1 — رفع المشروع لـ GitHub

#### الطريقة الأسهل: GitHub Desktop
1. نزّل **GitHub Desktop** من https://desktop.github.com
2. سجّل دخول بحساب GitHub
3. اضغط **File → Add Local Repository**
4. اختر المجلد: `c:\Users\RAO\Desktop\HomeMaids`
5. اضغط **Publish repository**:
   - الاسم: `althiqa-global` (أو ما تختار)
   - ✅ **Keep this code private** (لتبقى المفاتيح والإعدادات سرّية)
6. اضغط **Publish Repository**

#### الطريقة البديلة: عبر Git CLI
```powershell
cd c:\Users\RAO\Desktop\HomeMaids
# (Git مهيأ بالفعل و223+ ملف مُرفقة)
git remote add origin https://github.com/khalifak49-beep/althiqa-global.git
git push -u origin main
# (سيسألك عن اسم المستخدم/PAT — استخدم Personal Access Token من github.com/settings/tokens)
```

---

### الخطوة 2 — إنشاء حساب Render

1. افتح: https://render.com/register
2. اختر **"Sign up with GitHub"** (أسهل من البريد لأنه يربط الحسابين تلقائياً)
3. أعطِ Render إذن الوصول لمستودعاتك

---

### الخطوة 3 — النشر بضغطة واحدة (Blueprint)

المشروع فيه ملف `render.yaml` مُعدّ مسبقاً — Render سيستخدمه لإنشاء السيرفر + القاعدة تلقائياً.

1. في لوحة Render اضغط **New +** → **Blueprint**
2. اختر المستودع الذي رفعته
3. Render سيقرأ `render.yaml` ويعرض:
   - 🌐 **Web Service**: `althiqa` (Docker, free, Frankfurt)
   - 💾 **Database**: `althiqa-db` (PostgreSQL, free)
4. اضغط **Apply** → ابدأ النشر

النشر الأول يستغرق ~5-10 دقائق (بناء Docker image + تشغيل migrations + seed data).

---

### الخطوة 4 — تحديث إعدادات الأدمن بعد النشر

بعد ما النشر يكتمل (سيظهر رابطك مثل `https://althiqa.onrender.com`):

1. سجّل دخول كأدمن: `admin@homemaids.local` / `Admin@123456`
2. **غيّر كلمة سر الأدمن فوراً** (الافتراضية معروفة وغير آمنة)
3. افتح **/Admin/EmailSettings** → الصق App Password الخاص بـ Gmail
4. افتح **/Admin/PaymentGateways** → تأكد من مفاتيح Thawani
5. افتح **/Admin/Branding/Logo** → ارفع الشعار

---

### الخطوة 5 — منع السيرفر من النوم (اختياري لكن مهم)

السيرفر المجاني ينام بعد 15 دقيقة بلا نشاط. للحل المجاني:

1. سجّل في **UptimeRobot**: https://uptimerobot.com/signUp
2. أنشئ Monitor جديد:
   - النوع: **HTTP(s)**
   - URL: `https://althiqa.onrender.com` (رابطك من Render)
   - Interval: **5 دقائق**
3. UptimeRobot سيزور موقعك كل 5 دقائق → السيرفر يبقى صاحياً 24/7

---

## 🔁 التحديثات المستقبلية

عندما تعدّل الكود:
1. عدّل في جهازك المحلي
2. ارفع التغييرات: `git push` (أو زر **Push** في GitHub Desktop)
3. Render سيكتشف التغيير ويعيد البناء والنشر تلقائياً (~3-5 دقائق)

---

## 🆘 حل المشاكل الشائعة

| المشكلة | السبب | الحل |
|---------|------|------|
| البناء فشل في Render | كود لا يبني | افحص سجل Render، عادة خطأ syntax — صحّح وادفع git push |
| الصفحة بطيئة أول مرة (~50s) | السيرفر نائم | UptimeRobot يحلّها |
| Database connection failed | URL غير صحيح | تأكد أن `DATABASE_URL` env var موجودة (تُضاف تلقائياً من Blueprint) |
| OTP لا يصل | SMTP غير مُهيّأ | افتح /Admin/EmailSettings والصق App Password |
| Thawani لا يفتح | المفاتيح خاطئة | افتح /Admin/PaymentGateways وعدّلها |
| القاعدة فارغة | EnsureCreated لم يُشغّل seed | أعد تشغيل السيرفر من لوحة Render → Manual Deploy |

---

## 💰 الترقية للإنتاج الجاد

عندما ينمو موقعك:

| المكوّن | السعر | الفائدة |
|--------|------|---------|
| Web Service Starter | $7/شهر | لا نوم، أداء أفضل |
| PostgreSQL Starter | $7/شهر | لا انتهاء، نسخ احتياطي يومي |
| **المجموع** | **$14/شهر** | موقع احترافي كامل |

أو ترقَ لاحقاً إلى:
- **VPS** (Hetzner ~$5/شهر) لتحكم كامل
- **Azure App Service + Azure SQL** للإنتاج الكبير

---

## 📞 الدعم

- **توثيق Render**: https://render.com/docs
- **سجل البناء**: في لوحة Render → خدمتك → Logs
- **صحة النظام**: من موقعك المنشور افتح `/Admin/Health`
