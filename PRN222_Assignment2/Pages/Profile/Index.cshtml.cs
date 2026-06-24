using BusinessLayer.DTOs;
using BusinessLayer.Services;
using PRN222_Assignment2.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace PRN222_Assignment2.Pages.Profile;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IAuthService _authService;

    public IndexModel(IAuthService authService)
    {
        _authService = authService;
    }

    public UserDto? UserDto { get; set; }

    [BindProperty]
    public ChangePasswordViewModel PasswordInput { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(userIdStr, out var userId))
        {
            UserDto = await _authService.GetByIdAsync(userId);
            if (UserDto == null) return RedirectToPage("/Auth/Logout");
        }
        else
        {
            return RedirectToPage("/Auth/Login");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostChangePasswordAsync()
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Vui lòng kiểm tra lại thông tin đã nhập.";
            return RedirectToPage();
        }

        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(userIdStr, out var userId))
        {
            var success = await _authService.ChangePasswordAsync(userId, PasswordInput.CurrentPassword, PasswordInput.NewPassword);
            if (success)
            {
                TempData["Success"] = "Đổi mật khẩu thành công!";
            }
            else
            {
                TempData["Error"] = "Mật khẩu hiện tại không đúng hoặc có lỗi xảy ra.";
            }
        }

        return RedirectToPage();
    }
}
