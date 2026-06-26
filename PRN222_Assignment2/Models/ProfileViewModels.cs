using System.ComponentModel.DataAnnotations;

namespace PRN222_Assignment2.Models;

/// <summary>
/// ViewModel dùng ở Profile/Index — form đổi mật khẩu.
/// Dùng [Compare] để tự động kiểm tra ConfirmPassword khớp, thay vì check thủ công.
/// </summary>
public class ChangePasswordViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu hiện tại.")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu hiện tại")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới.")]
    [DataType(DataType.Password)]
    [StringLength(255, MinimumLength = 6, ErrorMessage = "Mật khẩu mới phải chứa ít nhất 6 ký tự.")]
    [Display(Name = "Mật khẩu mới")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng hãy xác nhận mật khẩu mới.")]
    [DataType(DataType.Password)]
    [Compare("NewPassword", ErrorMessage = "Mật khẩu xác nhận không khớp với mật khẩu mới.")]
    [Display(Name = "Xác nhận mật khẩu mới")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
