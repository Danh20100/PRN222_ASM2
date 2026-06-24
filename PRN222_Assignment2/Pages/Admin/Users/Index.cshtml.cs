using BusinessLayer.DTOs;
using BusinessLayer.Services;
using PRN222_Assignment2.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PRN222_Assignment2.Pages.Admin.Users;

[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly IAuthService _authService;

    public IndexModel(IAuthService authService)
    {
        _authService = authService;
    }

    public IEnumerable<UserDto> Users { get; set; } = new List<UserDto>();

    [BindProperty]
    public CreateUserViewModel Input { get; set; } = new();

    public async Task OnGetAsync()
    {
        Users = await _authService.GetAllUsersAsync();
    }

    public async Task<IActionResult> OnPostCreateUserAsync()
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Vui lòng kiểm tra lại thông tin đã nhập.";
            return RedirectToPage();
        }

        try
        {
            var dto = new CreateUserDto
            {
                FullName = Input.FullName,
                Username = Input.Username,
                Email = Input.Email,
                Password = Input.Password,
                Role = Input.Role
            };
            await _authService.RegisterAsync(dto);
            TempData["Success"] = $"Đã tạo user {Input.Username} thành công!";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleActiveAsync(int userId)
    {
        var result = await _authService.ToggleActiveAsync(userId);
        if (result) TempData["Success"] = "Đã cập nhật trạng thái User!";
        else TempData["Error"] = "Cập nhật thất bại!";
        
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostImportCsvAsync(IFormFile csvFile)
    {
        if (csvFile == null || csvFile.Length == 0)
        {
            TempData["Error"] = "Vui lòng chọn file CSV.";
            return RedirectToPage();
        }

        try
        {
            using var stream = csvFile.OpenReadStream();
            var (successCount, skipCount) = await _authService.ImportUsersFromCsvAsync(stream);
            
            TempData["Success"] = $"Import thành công {successCount} users. Bỏ qua {skipCount} bản ghi lỗi/trùng lặp.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = "Lỗi khi import file: " + ex.Message;
        }

        return RedirectToPage();
    }
}
