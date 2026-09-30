namespace CurriculumGenerator.Models;

public class ResumeSummaryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? TemplateId { get; set; }
    public bool HasPdf { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class ResumeDetailDto : ResumeSummaryDto
{
    public object? Data { get; set; }
}

public class CreateResumeRequest
{
    public string Name { get; set; } = string.Empty;
    public int? TemplateId { get; set; }
    public object? Data { get; set; }
}
