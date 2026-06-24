using System.ComponentModel.DataAnnotations;

namespace PRN222_Assignment2.Models;

/// <summary>
/// ViewModel dùng ở trang Admin/Users — form tạo user mới.
/// Tách biệt với CreateUserDto của BusinessLayer.
/// </summary>
public class CreateUserViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
    [StringLength(150, ErrorMessage = "Họ tên tối đa 150 ký tự.")]
    [Display(Name = "Họ và tên")]
    public string? FullName { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Tên đăng nhập phải từ 3–100 ký tự.")]
    [RegularExpression(@"^[a-zA-Z0-9_\.]+$", ErrorMessage = "Tên đăng nhập chỉ được chứa chữ, số, dấu gạch dưới hoặc chấm.")]
    [Display(Name = "Tên đăng nhập")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập email.")]
    [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
    [StringLength(200, ErrorMessage = "Email tối đa 200 ký tự.")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
    [DataType(DataType.Password)]
    [StringLength(255, MinimumLength = 6, ErrorMessage = "Mật khẩu phải từ 6 ký tự trở lên.")]
    [Display(Name = "Mật khẩu")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn vai trò.")]
    [RegularExpression(@"^(Admin|Teacher|Student)$", ErrorMessage = "Vai trò không hợp lệ.")]
    [Display(Name = "Vai trò")]
    public string Role { get; set; } = "Student";
}
