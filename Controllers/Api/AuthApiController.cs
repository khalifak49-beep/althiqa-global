using HomeMaids.Data;
using HomeMaids.Dtos;
using HomeMaids.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HomeMaids.Controllers.Api;

[ApiController]
[Route("api/auth")]
[Microsoft.AspNetCore.Mvc.IgnoreAntiforgeryToken]
public class AuthApiController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<AuthApiController> _logger;

    public AuthApiController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IWebHostEnvironment env,
        ILogger<AuthApiController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _env = env;
        _logger = logger;
    }

    /// <summary>
    /// Direct password-based registration is disabled in production — customers must register
    /// via phone OTP (/Account/Phone) or email OTP (/Account/Email) to prove ownership.
    /// </summary>
    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<TokenDto>>> Register([FromBody] RegisterDto dto)
    {
        if (!_env.IsDevelopment())
            return StatusCode(StatusCodes.Status410Gone,
                ApiResponse<TokenDto>.Fail("Direct registration is disabled. Use /Account/Phone or /Account/Email."));

        var user = new ApplicationUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            EmailConfirmed = false,
            PhoneNumber = dto.PhoneNumber,
            FullName = dto.FullName,
            IsActive = true
        };
        var res = await _userManager.CreateAsync(user, dto.Password);
        if (!res.Succeeded) return BadRequest(ApiResponse<TokenDto>.Fail(string.Join("; ", res.Errors.Select(e => e.Description))));
        await _userManager.AddToRoleAsync(user, DbInitializer.CustomerRole);
        return Ok(ApiResponse<TokenDto>.Ok(new TokenDto
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email!,
            Role = DbInitializer.CustomerRole
        }, "Registered (dev only)"));
    }

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<TokenDto>>> Login([FromBody] LoginDto dto)
    {
        // Uses PasswordSignInAsync (NOT CheckPasswordAsync) so failed-attempts count toward lockout.
        var result = await _signInManager.PasswordSignInAsync(dto.Email, dto.Password, isPersistent: true, lockoutOnFailure: true);
        if (result.IsLockedOut)
        {
            _logger.LogWarning("API login lockout for {Email}", dto.Email);
            return Unauthorized(ApiResponse<TokenDto>.Fail("Account temporarily locked. Try again in 15 minutes."));
        }
        if (!result.Succeeded)
            return Unauthorized(ApiResponse<TokenDto>.Fail("Invalid credentials"));

        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null) return Unauthorized(ApiResponse<TokenDto>.Fail("Invalid credentials"));
        var roles = await _userManager.GetRolesAsync(user);
        return Ok(ApiResponse<TokenDto>.Ok(new TokenDto
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email!,
            Role = roles.FirstOrDefault()
        }));
    }
}
