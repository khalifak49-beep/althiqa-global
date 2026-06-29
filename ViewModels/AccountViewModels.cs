using System.ComponentModel.DataAnnotations;

namespace HomeMaids.ViewModels;

public class RegisterViewModel
{
    [Required(ErrorMessage = "الاسم مطلوب"), StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "البريد مطلوب"), EmailAddress(ErrorMessage = "بريد غير صالح")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "رقم الجوال مطلوب"), Phone(ErrorMessage = "رقم غير صالح")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "كلمة المرور مطلوبة")]
    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "كلمة المرور 8 أحرف على الأقل وتحتوي على حرف كبير وصغير ورقم ورمز")]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "كلمتا المرور غير متطابقتين")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class LoginViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}

public class ProfileViewModel
{
    [Required, StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [EmailAddress]
    public string? Email { get; set; }

    [Phone]
    public string? PhoneNumber { get; set; }

    [StringLength(250)]
    public string? DefaultAddress { get; set; }

    public int LoyaltyPoints { get; set; }

    public string? AvatarUrl { get; set; }
}

public class PhoneOtpViewModel
{
    [Required(ErrorMessage = "أدخل رقم الجوال")]
    [StringLength(20)]
    public string Phone { get; set; } = string.Empty;

    [StringLength(100)]
    public string? FullName { get; set; }

    [StringLength(6)]
    public string? Code { get; set; }

    public string? ReturnUrl { get; set; }

    // Dev visibility (filled by SendOtp action when LogSmsSender is enabled)
    public string? DevCode { get; set; }
    public DateTime? ExpiresAt { get; set; }

    /// <summary>True if the phone is already registered — login flow, no FullName needed.</summary>
    public bool IsExisting { get; set; }
    public string? ExistingName { get; set; }

    /// <summary>Optional real email captured during phone-OTP signup so we have full customer data.</summary>
    [EmailAddress(ErrorMessage = "بريد غير صحيح")]
    [StringLength(120)]
    public string? Email { get; set; }
}

public class EmailOtpViewModel
{
    [Required(ErrorMessage = "أدخل بريدك الإلكتروني")]
    [EmailAddress(ErrorMessage = "بريد غير صحيح")]
    [StringLength(120)]
    public string Email { get; set; } = string.Empty;

    [StringLength(100)]
    public string? FullName { get; set; }

    [StringLength(6)]
    public string? Code { get; set; }

    public string? ReturnUrl { get; set; }
    public string? DevCode { get; set; }
    public DateTime? ExpiresAt { get; set; }

    /// <summary>True if the email is already registered — login flow, no FullName needed.</summary>
    public bool IsExisting { get; set; }
    public string? ExistingName { get; set; }

    /// <summary>Optional phone captured during email-OTP signup so we have full customer data.</summary>
    [Phone(ErrorMessage = "رقم جوال غير صحيح")]
    [StringLength(20)]
    public string? PhoneNumber { get; set; }
}

public class ChangePasswordViewModel
{
    [Required, DataType(DataType.Password)]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), StringLength(100, MinimumLength = 8)]
    public string NewPassword { get; set; } = string.Empty;

    [DataType(DataType.Password), Compare(nameof(NewPassword))]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class ChangeEmailViewModel
{
    [Required(ErrorMessage = "البريد الإلكتروني الجديد مطلوب")]
    [EmailAddress(ErrorMessage = "بريد غير صالح")]
    [StringLength(120)]
    public string NewEmail { get; set; } = string.Empty;

    [Required(ErrorMessage = "كلمة المرور الحالية مطلوبة للتحقق")]
    [DataType(DataType.Password)]
    public string CurrentPassword { get; set; } = string.Empty;
}

public class ForgotPasswordViewModel
{
    [Required(ErrorMessage = "أدخل بريدك الإلكتروني")]
    [EmailAddress(ErrorMessage = "بريد غير صالح")]
    [StringLength(120)]
    public string Email { get; set; } = string.Empty;

    public string? DevCode { get; set; }
}

public class ResetPasswordViewModel
{
    [Required, EmailAddress, StringLength(120)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "أدخل الرمز")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "الرمز 6 أرقام")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "كلمة المرور الجديدة مطلوبة")]
    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "كلمة المرور 8 أحرف على الأقل")]
    public string NewPassword { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "كلمتا المرور غير متطابقتين")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
