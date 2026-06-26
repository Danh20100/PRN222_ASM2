using System.ComponentModel.DataAnnotations;

namespace PRN222_Assignment2.Models;

/// <summary>
/// ViewModel dành cho form đăng nhập — tách biệt với LoginDto của BusinessLayer.
/// Chứa DataAnnotations để ASP.NET Core tự động validate phía server.
/// </summary>
public class LoginViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Tên đăng nhập phải từ 3–100 ký tự.")]
    [Display(Name = "Tên đăng nhập")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng điền mật khẩu.")]
    [DataType(DataType.Password)]
    [StringLength(255, MinimumLength = 6, ErrorMessage = "Mật khẩu phải chứa ít nhất 6 ký tự.")]
    [Display(Name = "Mật khẩu")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Ghi nhớ đăng nhập")]
    public bool RememberMe { get; set; }
}
