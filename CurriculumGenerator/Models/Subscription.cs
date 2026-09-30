namespace CurriculumGenerator.Models;

public class Subscription
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Plan { get; set; } = "free";
    public string Status { get; set; } = "active";
    public string? MercadoPagoId { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
