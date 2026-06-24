using System.ComponentModel.DataAnnotations;

namespace PRN222_Assignment2.Models;

/// <summary>
/// ViewModel dùng ở Benchmark/Create — form tạo Experiment mới.
/// Tách biệt với CreateExperimentDto của BusinessLayer.
/// </summary>
public class CreateExperimentViewModel
{
    [Required(ErrorMessage = "Hãy nhập tên Experiment.")]
    [StringLength(200, MinimumLength = 3, ErrorMessage = "Tên Experiment phải từ 3–200 ký tự.")]
    [Display(Name = "Tên Experiment")]
    public string ExperimentName { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Mô tả tối đa 1000 ký tự.")]
    [Display(Name = "Mô tả")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Xin hãy chọn môn học.")]
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn môn học hợp lệ.")]
    [Display(Name = "Môn học")]
    public int SubjectId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn Embedding Model.")]
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn Embedding Model hợp lệ.")]
    [Display(Name = "Embedding Model")]
    public int EmbeddingModelId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn AI Model.")]
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn AI Model hợp lệ.")]
    [Display(Name = "AI Model")]
    public int AiModelId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn Chunking Strategy.")]
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn Chunking Strategy hợp lệ.")]
    [Display(Name = "Chunking Strategy")]
    public int ChunkingStrategyId { get; set; }

    [Range(1, 20, ErrorMessage = "Top-K phải từ 1 đến 20.")]
    [Display(Name = "Top-K (số chunks tham chiếu)")]
    public int TopK { get; set; } = 3;
}

/// <summary>
/// ViewModel dùng ở Benchmark/Details — form thêm Test Case mới.
/// Gom 2 string rời rạc thành 1 object có validation.
/// </summary>
public class AddTestCaseViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập câu hỏi.")]
    [StringLength(2000, MinimumLength = 5, ErrorMessage = "Câu hỏi phải từ 5–2000 ký tự.")]
    [Display(Name = "Câu hỏi")]
    public string Question { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập câu trả lời mong đợi.")]
    [StringLength(5000, MinimumLength = 5, ErrorMessage = "Câu trả lời phải từ 5–5000 ký tự.")]
    [Display(Name = "Câu trả lời mong đợi")]
    public string ExpectedAnswer { get; set; } = string.Empty;
}
