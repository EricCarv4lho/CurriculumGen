namespace CurriculumGenerator.Models;

public class UsageLog
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.UtcNow;
}
