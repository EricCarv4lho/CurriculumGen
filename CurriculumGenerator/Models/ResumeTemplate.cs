namespace CurriculumGenerator.Models;

public class ResumeTemplate
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsPremium { get; set; }
    public string? PreviewUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
