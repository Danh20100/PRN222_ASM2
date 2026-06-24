using BusinessLayer.DTOs;
using BusinessLayer.Services;
using PRN222_Assignment2.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace PRN222_Assignment2.Pages.Auth;

[AllowAnonymous]
public class LoginModel : PageModel
{
    private readonly IAuthService _authService;

    public LoginModel(IAuthService authService)
    {
        _authService = authService;
    }

    [BindProperty]
    public LoginViewModel Input { get; set; } = new();

    public string? ErrorMessage { get; set; }
    public string? ReturnUrl { get; set; }

    public void OnGet(string? returnUrl = null)
    {
        ReturnUrl = returnUrl ?? Url.Content("~/");
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        if (!ModelState.IsValid)
        {
            ErrorMessage = "Vui lòng điền đầy đủ thông tin.";
            return Page();
        }

        // Map ViewModel → DTO trước khi gọi BusinessLayer
        var loginDto = new LoginDto
        {
            Username = Input.Username,
            Password = Input.Password,
            RememberMe = Input.RememberMe
        };
        var user = await _authService.LoginAsync(loginDto);
        if (user is null)
        {
            ErrorMessage = "Tên đăng nhập hoặc mật khẩu không đúng.";
            return Page();
        }

        // Build claims
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role),
            new("FullName", user.FullName ?? user.Username)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        var authProps = new AuthenticationProperties
        {
            IsPersistent = loginDto.RememberMe,
            ExpiresUtc = loginDto.RememberMe
                ? DateTimeOffset.UtcNow.AddDays(7)
                : DateTimeOffset.UtcNow.AddHours(8)
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            authProps);

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) && returnUrl != "/")
        {
            return LocalRedirect(returnUrl);
        }

        if (user.Role == "Admin")
        {
            return LocalRedirect("/Admin/Users");
        }
        else if (user.Role == "Teacher")
        {
            return LocalRedirect("/Subjects");
        }
        else
        {
            // Student
            if (loginDto.Password == "123456" || loginDto.Password == user.Username) 
            {
                // Force change password
                return LocalRedirect("/Profile?forceChangePassword=true");
            }
            return LocalRedirect("/Chat");
        }
    }
}
