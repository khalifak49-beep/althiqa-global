using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace HomeMaids.Controllers;

public class CultureController : Controller
{
    /// <summary>
    /// Sets the user's language preference via cookie, then redirects back.
    /// Usage: /Culture/Set?culture=en&returnUrl=/
    /// </summary>
    [HttpGet]
    public IActionResult Set(string culture, string? returnUrl)
    {
        if (culture != "ar" && culture != "en") culture = "ar";

        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
            new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                IsEssential = true,
                HttpOnly = false,
                SameSite = SameSiteMode.Lax
            });

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);
        return Redirect("/");
    }
}
