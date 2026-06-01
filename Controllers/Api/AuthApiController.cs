using HomeMaids.Data;
using HomeMaids.Dtos;
using HomeMaids.Models;
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

    public AuthApiController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<TokenDto>>> Register([FromBody] RegisterDto dto)
    {
        var user = new ApplicationUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            EmailConfirmed = true,
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
        }, "Registered"));
    }

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<TokenDto>>> Login([FromBody] LoginDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null) return Unauthorized(ApiResponse<TokenDto>.Fail("Invalid credentials"));
        var ok = await _userManager.CheckPasswordAsync(user, dto.Password);
        if (!ok) return Unauthorized(ApiResponse<TokenDto>.Fail("Invalid credentials"));

        await _signInManager.SignInAsync(user, isPersistent: true);
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
