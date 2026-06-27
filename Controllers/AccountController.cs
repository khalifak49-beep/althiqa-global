using HomeMaids.Data;
using HomeMaids.Models;
using HomeMaids.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HomeMaids.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ApplicationDbContext _db;
    private readonly Services.IOtpService _otp;
    private readonly Services.IEmailOtpSender _emailOtp;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ApplicationDbContext db,
        Services.IOtpService otp,
        Services.IEmailOtpSender emailOtp,
        ILogger<AccountController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _db = db;
        _otp = otp;
        _emailOtp = emailOtp;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost]
    public async Task<IActionResult> Register([Bind("FullName,Email,PhoneNumber,Password,ConfirmPassword")] RegisterViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var email = vm.Email.Trim().ToLowerInvariant();
        if (await _userManager.FindByEmailAsync(email) != null)
        {
            ModelState.AddModelError(nameof(vm.Email), "هذا البريد مسجّل بالفعل.");
            return View(vm);
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            // Note: Email confirmation is required for sensitive ops — kept true for UX simplicity
            // but full identity proofing happens through OTP flows at /Account/Phone or /Account/Email.
            EmailConfirmed = false,
            PhoneNumber = vm.PhoneNumber?.Trim(),
            PhoneNumberConfirmed = false,
            FullName = vm.FullName.Trim(),
            IsActive = true
        };

        var result = await _userManager.CreateAsync(user, vm.Password);
        if (!result.Succeeded)
        {
            foreach (var err in result.Errors) ModelState.AddModelError(string.Empty, err.Description);
            return View(vm);
        }

        await _userManager.AddToRoleAsync(user, DbInitializer.CustomerRole);
        await _signInManager.SignInAsync(user, isPersistent: false);
        _logger.LogInformation("New customer registered: {Email}", email);
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl) => View(new LoginViewModel { ReturnUrl = returnUrl });

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var result = await _signInManager.PasswordSignInAsync(vm.Email, vm.Password, vm.RememberMe, lockoutOnFailure: true);
        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, "تم قفل الحساب مؤقتاً بسبب محاولات دخول كثيرة فاشلة. حاول بعد 15 دقيقة.");
            _logger.LogWarning("Account locked out: {Email}", vm.Email);
            return View(vm);
        }
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, "بيانات الدخول غير صحيحة.");
            return View(vm);
        }

        if (!string.IsNullOrEmpty(vm.ReturnUrl) && Url.IsLocalUrl(vm.ReturnUrl))
            return Redirect(vm.ReturnUrl);

        var user = await _userManager.FindByEmailAsync(vm.Email);
        if (user != null && await _userManager.IsInRoleAsync(user, DbInitializer.AdminRole))
            return RedirectToAction("Index", "Dashboard", new { area = "Admin" });

        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();
        return View(new ProfileViewModel
        {
            FullName = user.FullName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            DefaultAddress = user.DefaultAddress,
            LoyaltyPoints = user.LoyaltyPoints,
            AvatarUrl = user.AvatarUrl
        });
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Profile([Bind("FullName,PhoneNumber,DefaultAddress,AvatarUrl")] ProfileViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        user.FullName = vm.FullName?.Trim() ?? user.FullName;
        user.PhoneNumber = vm.PhoneNumber?.Trim();
        user.DefaultAddress = vm.DefaultAddress?.Trim();

        // Only allow avatar URLs that are local uploads to prevent SSRF/XSS through external images
        if (!string.IsNullOrEmpty(vm.AvatarUrl) && Uri.TryCreate(vm.AvatarUrl, UriKind.Relative, out _))
            user.AvatarUrl = vm.AvatarUrl;

        await _userManager.UpdateAsync(user);
        TempData["Success"] = "تم تحديث الملف الشخصي.";
        return RedirectToAction(nameof(Profile));
    }

    [Authorize]
    [HttpGet]
    public IActionResult ChangePassword() => View(new ChangePasswordViewModel());

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var result = await _userManager.ChangePasswordAsync(user, vm.CurrentPassword, vm.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var err in result.Errors) ModelState.AddModelError(string.Empty, err.Description);
            return View(vm);
        }

        await _signInManager.RefreshSignInAsync(user);
        TempData["Success"] = "تم تغيير كلمة المرور.";
        return RedirectToAction(nameof(Profile));
    }

    [Authorize]
    public async Task<IActionResult> Favorites()
    {
        var userId = _userManager.GetUserId(User);
        var favorites = await _db.Favorites
            .Include(f => f.Worker).ThenInclude(w => w!.Service)
            .Where(f => f.UserId == userId)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();
        return View(favorites);
    }

    public IActionResult AccessDenied() => View();

    // ============================================================
    //  Phone-based registration / login (OTP)
    // ============================================================

    [HttpGet]
    public IActionResult Phone() => View(new ViewModels.PhoneOtpViewModel());

    [HttpPost]
    public async Task<IActionResult> SendOtp(ViewModels.PhoneOtpViewModel vm)
    {
        if (string.IsNullOrWhiteSpace(vm.Phone))
        {
            ModelState.AddModelError(nameof(vm.Phone), "أدخل رقم الجوال.");
            return View(nameof(Phone), vm);
        }

        var result = await _otp.SendAsync(vm.Phone);
        if (!result.Success)
        {
            ModelState.AddModelError("", result.Error ?? "تعذر إرسال الرمز.");
            return View(nameof(Phone), vm);
        }

        vm.Phone = _otp.NormalizePhone(vm.Phone);
        vm.DevCode = result.DevCode;
        vm.ExpiresAt = result.ExpiresAt;

        // Detect if this phone already has a user — switches the flow to login (no name needed)
        var existing = await _userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == vm.Phone);
        if (existing != null)
        {
            vm.IsExisting = true;
            vm.ExistingName = existing.FullName;
            TempData["Success"] = $"أهلاً بعودتك {existing.FullName} — أرسلنا رمز الدخول إلى جوالك.";
        }
        else
        {
            TempData["Success"] = "تم إرسال رمز التحقق إلى رقم جوالك.";
        }
        return View(nameof(VerifyOtp), vm);
    }

    [HttpPost]
    public async Task<IActionResult> VerifyOtp(ViewModels.PhoneOtpViewModel vm)
    {
        if (string.IsNullOrWhiteSpace(vm.Phone) || string.IsNullOrWhiteSpace(vm.Code))
        {
            ModelState.AddModelError("", "أدخل الرمز.");
            return View(vm);
        }

        var verify = await _otp.VerifyAsync(vm.Phone, vm.Code);
        if (!verify.Success)
        {
            ModelState.AddModelError(nameof(vm.Code), verify.Error ?? "رمز غير صحيح.");
            return View(vm);
        }

        var phone = _otp.NormalizePhone(vm.Phone);

        // Find existing user by phone, otherwise create new (requires FullName)
        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phone);

        if (user == null)
        {
            if (string.IsNullOrWhiteSpace(vm.FullName))
            {
                ModelState.AddModelError(nameof(vm.FullName), "أدخل اسمك الكامل للتسجيل لأول مرة.");
                return View(vm);
            }
            // Use real email if user supplied it, otherwise a synthetic one (Identity requires unique email)
            string userEmail;
            if (!string.IsNullOrWhiteSpace(vm.Email) && vm.Email.Contains('@'))
            {
                userEmail = vm.Email.Trim().ToLowerInvariant();
                // Make sure it's not taken by someone else
                if (await _userManager.FindByEmailAsync(userEmail) != null)
                {
                    ModelState.AddModelError(nameof(vm.Email), "هذا البريد مسجّل لحساب آخر.");
                    return View(vm);
                }
            }
            else
            {
                userEmail = $"{phone.TrimStart('+')}@phone.al-thiqa.local";
            }
            user = new ApplicationUser
            {
                UserName = phone,
                Email = userEmail,
                EmailConfirmed = true,
                PhoneNumber = phone,
                PhoneNumberConfirmed = true,
                FullName = vm.FullName.Trim(),
                IsActive = true
            };
            // Random secure password (user never uses it; future logins via OTP)
            var randomPassword = "Otp@" + Guid.NewGuid().ToString("N")[..16];
            var create = await _userManager.CreateAsync(user, randomPassword);
            if (!create.Succeeded)
            {
                foreach (var err in create.Errors) ModelState.AddModelError("", err.Description);
                return View(vm);
            }
            await _userManager.AddToRoleAsync(user, DbInitializer.CustomerRole);
            _logger.LogInformation("New customer created via phone OTP: {Phone}", phone);
        }
        else if (!user.PhoneNumberConfirmed)
        {
            user.PhoneNumberConfirmed = true;
            await _userManager.UpdateAsync(user);
        }

        await _signInManager.SignInAsync(user, isPersistent: true);

        if (!string.IsNullOrEmpty(vm.ReturnUrl) && Url.IsLocalUrl(vm.ReturnUrl))
            return Redirect(vm.ReturnUrl);

        if (await _userManager.IsInRoleAsync(user, DbInitializer.AdminRole))
            return RedirectToAction("Index", "Dashboard", new { area = "Admin" });

        return RedirectToAction("Index", "Home");
    }

    // ============================================================
    //  Email-based registration / login (OTP via Gmail SMTP — FREE)
    // ============================================================

    [HttpGet]
    public IActionResult Email() => View(new ViewModels.EmailOtpViewModel());

    [HttpPost]
    public async Task<IActionResult> SendEmailOtp(ViewModels.EmailOtpViewModel vm)
    {
        if (string.IsNullOrWhiteSpace(vm.Email) || !vm.Email.Contains('@'))
        {
            ModelState.AddModelError(nameof(vm.Email), "أدخل بريداً إلكترونياً صحيحاً.");
            return View(nameof(Email), vm);
        }

        var email = vm.Email.Trim().ToLowerInvariant();

        // Throttle: one OTP per 30s
        var recent = await _db.PhoneOtps
            .Where(o => o.Phone == "email:" + email)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync();
        if (recent != null && (DateTime.UtcNow - recent.CreatedAt).TotalSeconds < 30)
        {
            var wait = 30 - (int)(DateTime.UtcNow - recent.CreatedAt).TotalSeconds;
            ModelState.AddModelError("", $"يرجى الانتظار {wait} ثانية قبل طلب رمز جديد.");
            return View(nameof(Email), vm);
        }

        var code = Random.Shared.Next(100000, 999999).ToString("D6");
        _db.PhoneOtps.Add(new Models.PhoneOtp
        {
            Phone = "email:" + email,
            Code = code,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            Attempts = 0,
            Used = false
        });
        await _db.SaveChangesAsync();

        var devCode = await _emailOtp.SendOtpAsync(email, code);

        vm.Email = email;
        vm.DevCode = devCode;
        vm.ExpiresAt = DateTime.UtcNow.AddMinutes(5);

        // Detect existing user — switches to login flow (no name needed)
        var existing = await _userManager.FindByEmailAsync(email);
        if (existing != null)
        {
            vm.IsExisting = true;
            vm.ExistingName = existing.FullName;
            TempData["Success"] = $"أهلاً بعودتك {existing.FullName} — أرسلنا رمز الدخول إلى بريدك.";
        }
        else
        {
            TempData["Success"] = "تم إرسال رمز التحقق إلى بريدك.";
        }
        return View(nameof(VerifyEmailOtp), vm);
    }

    [HttpPost]
    public async Task<IActionResult> VerifyEmailOtp(ViewModels.EmailOtpViewModel vm)
    {
        if (string.IsNullOrWhiteSpace(vm.Email) || string.IsNullOrWhiteSpace(vm.Code))
        {
            ModelState.AddModelError("", "أدخل الرمز.");
            return View(vm);
        }

        var email = vm.Email.Trim().ToLowerInvariant();
        var otp = await _db.PhoneOtps
            .Where(o => o.Phone == "email:" + email && !o.Used)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync();

        if (otp == null)
        {
            ModelState.AddModelError(nameof(vm.Code), "لم يتم إرسال رمز لهذا البريد.");
            return View(vm);
        }
        if (otp.ExpiresAt < DateTime.UtcNow)
        {
            ModelState.AddModelError(nameof(vm.Code), "انتهت صلاحية الرمز.");
            return View(vm);
        }
        if (otp.Attempts >= 5)
        {
            ModelState.AddModelError(nameof(vm.Code), "تجاوزت الحد المسموح من المحاولات.");
            return View(vm);
        }
        otp.Attempts++;
        if (otp.Code != vm.Code.Trim())
        {
            await _db.SaveChangesAsync();
            ModelState.AddModelError(nameof(vm.Code), "الرمز غير صحيح.");
            return View(vm);
        }
        otp.Used = true;
        await _db.SaveChangesAsync();

        // Find or create user by email
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            if (string.IsNullOrWhiteSpace(vm.FullName))
            {
                ModelState.AddModelError(nameof(vm.FullName), "أدخل اسمك الكامل للتسجيل لأول مرة.");
                return View(vm);
            }
            // Save optional phone too so customer profile is complete
            string? phoneToSave = null;
            if (!string.IsNullOrWhiteSpace(vm.PhoneNumber))
            {
                var normalized = _otp.NormalizePhone(vm.PhoneNumber);
                if (await _userManager.Users.AnyAsync(u => u.PhoneNumber == normalized))
                {
                    ModelState.AddModelError(nameof(vm.PhoneNumber), "هذا الجوال مسجّل لحساب آخر.");
                    return View(vm);
                }
                phoneToSave = normalized;
            }
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                PhoneNumber = phoneToSave,
                PhoneNumberConfirmed = phoneToSave != null,
                FullName = vm.FullName.Trim(),
                IsActive = true
            };
            var randomPwd = "Otp@" + Guid.NewGuid().ToString("N")[..16];
            var create = await _userManager.CreateAsync(user, randomPwd);
            if (!create.Succeeded)
            {
                foreach (var err in create.Errors) ModelState.AddModelError("", err.Description);
                return View(vm);
            }
            await _userManager.AddToRoleAsync(user, DbInitializer.CustomerRole);
            _logger.LogInformation("New customer created via Email OTP: {Email}", email);
        }
        else if (!user.EmailConfirmed)
        {
            user.EmailConfirmed = true;
            await _userManager.UpdateAsync(user);
        }

        await _signInManager.SignInAsync(user, isPersistent: true);

        if (!string.IsNullOrEmpty(vm.ReturnUrl) && Url.IsLocalUrl(vm.ReturnUrl))
            return Redirect(vm.ReturnUrl);
        if (await _userManager.IsInRoleAsync(user, DbInitializer.AdminRole))
            return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
        return RedirectToAction("Index", "Home");
    }

    // ============================================================
    //  Forgot password — sends a 6-digit code by email, then lets the
    //  user choose a new password without knowing the old one.
    // ============================================================

    [HttpGet]
    public IActionResult ForgotPassword() => View(new ViewModels.ForgotPasswordViewModel());

    [HttpPost]
    public async Task<IActionResult> ForgotPassword(ViewModels.ForgotPasswordViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var email = vm.Email.Trim().ToLowerInvariant();

        // Always pretend the email was sent (don't reveal whether the email exists).
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            _logger.LogInformation("Password reset requested for unknown email {Email}", email);
            TempData["Success"] = "إذا كان البريد مسجلاً، سيصلك رمز الإعادة.";
            return RedirectToAction(nameof(ResetPassword), new { email });
        }

        // Throttle
        var recent = await _db.PhoneOtps
            .Where(o => o.Phone == "reset:" + email)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync();
        if (recent != null && (DateTime.UtcNow - recent.CreatedAt).TotalSeconds < 30)
        {
            var wait = 30 - (int)(DateTime.UtcNow - recent.CreatedAt).TotalSeconds;
            ModelState.AddModelError("", $"يرجى الانتظار {wait} ثانية قبل طلب رمز جديد.");
            return View(vm);
        }

        var code = Random.Shared.Next(100000, 999999).ToString("D6");
        _db.PhoneOtps.Add(new Models.PhoneOtp
        {
            Phone = "reset:" + email,
            Code = code,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
            Attempts = 0,
            Used = false
        });
        await _db.SaveChangesAsync();

        var devCode = await _emailOtp.SendOtpAsync(email, code);
        _logger.LogInformation("Password reset code sent to {Email}", email);

        TempData["Success"] = "أرسلنا رمز الإعادة إلى بريدك. تحقق من Spam/Junk أيضاً.";
        return RedirectToAction(nameof(ResetPassword), new { email, dev = devCode });
    }

    [HttpGet]
    public IActionResult ResetPassword(string? email, string? dev)
    {
        return View(new ViewModels.ResetPasswordViewModel
        {
            Email = email ?? string.Empty
        });
    }

    [HttpPost]
    public async Task<IActionResult> ResetPassword(ViewModels.ResetPasswordViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var email = vm.Email.Trim().ToLowerInvariant();
        var otp = await _db.PhoneOtps
            .Where(o => o.Phone == "reset:" + email && !o.Used)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync();

        if (otp == null)
        {
            ModelState.AddModelError(nameof(vm.Code), "لم يتم إرسال رمز لهذا البريد.");
            return View(vm);
        }
        if (otp.ExpiresAt < DateTime.UtcNow)
        {
            ModelState.AddModelError(nameof(vm.Code), "انتهت صلاحية الرمز.");
            return View(vm);
        }
        if (otp.Attempts >= 5)
        {
            ModelState.AddModelError(nameof(vm.Code), "تجاوزت الحد المسموح من المحاولات.");
            return View(vm);
        }
        otp.Attempts++;
        if (otp.Code != vm.Code.Trim())
        {
            await _db.SaveChangesAsync();
            ModelState.AddModelError(nameof(vm.Code), "الرمز غير صحيح.");
            return View(vm);
        }

        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            ModelState.AddModelError("", "حساب غير موجود.");
            return View(vm);
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, vm.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var err in result.Errors) ModelState.AddModelError("", err.Description);
            return View(vm);
        }

        otp.Used = true;
        await _db.SaveChangesAsync();

        // Unlock the account if it was locked due to failed attempts
        if (await _userManager.IsLockedOutAsync(user))
        {
            await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow);
            await _userManager.ResetAccessFailedCountAsync(user);
        }

        _logger.LogInformation("Password reset successfully for {Email}", email);
        TempData["Success"] = "تم تغيير كلمة المرور. يمكنك الدخول الآن.";
        return RedirectToAction(nameof(Login));
    }
}
