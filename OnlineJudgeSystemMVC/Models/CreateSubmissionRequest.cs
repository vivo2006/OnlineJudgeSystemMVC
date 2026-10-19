using System.ComponentModel.DataAnnotations;

namespace OnlineJudgeSystemMVC.Models;

public class CreateSubmissionRequest
{
    [Range(1, int.MaxValue)]
    public int ProblemId { get; set; }
    [Required, StringLength(50000, MinimumLength = 1)]
    public string Code { get; set; } = "";
    [Range(1, 1000)]
    public int LanguageId { get; set; }
}
