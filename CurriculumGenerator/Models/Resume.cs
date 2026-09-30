namespace CurriculumGenerator.Models;

public class Resume
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int? TemplateId { get; set; }
    public string Data { get; set; } = "{}";
    public byte[]? PdfData { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
