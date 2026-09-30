namespace CurriculumGenerator.Models;

public static class PlanConfig
{
    public static readonly Dictionary<string, PlanDefinition> Plans = new()
    {
        ["free"] = new PlanDefinition
        {
            Name = "Free",
            MaxResumes = 1,
            MaxTemplates = 1,
            MaxDownloadsPerMonth = 3,
            MaxTranslations = 0,
            MaxAiImproves = 0,
            MaxCoverLetters = 0,
            TemplatesAvailable = new[] { "classic" },
            PriceMonthly = 0
        },
        ["pro"] = new PlanDefinition
        {
            Name = "Pro",
            MaxResumes = null,
            MaxTemplates = null,
            MaxDownloadsPerMonth = null,
            MaxTranslations = 50,
            MaxAiImproves = 50,
            MaxCoverLetters = 10,
            TemplatesAvailable = new[] { "classic", "modern", "executive", "creative" },
            PriceMonthly = 2490
        },
        ["one_time"] = new PlanDefinition
        {
            Name = "Avulso",
            MaxResumes = 0,
            MaxTemplates = null,
            MaxDownloadsPerMonth = 1,
            MaxTranslations = 0,
            MaxAiImproves = 1,
            MaxCoverLetters = 0,
            TemplatesAvailable = new[] { "classic", "modern", "executive", "creative" },
            PriceMonthly = 3990
        }
    };

    public static PlanDefinition Get(string plan) =>
        Plans.TryGetValue(plan, out var p) ? p : Plans["free"];

    public static bool CanUseTemplate(string plan, string template) =>
        Get(plan).TemplatesAvailable.Contains(template);
}

public class PlanDefinition
{
    public string Name { get; set; } = "";
    public int? MaxResumes { get; set; }
    public int? MaxTemplates { get; set; }
    public int? MaxDownloadsPerMonth { get; set; }
    public int? MaxTranslations { get; set; }
    public int? MaxAiImproves { get; set; }
    public int? MaxCoverLetters { get; set; }
    public string[] TemplatesAvailable { get; set; } = Array.Empty<string>();
    public int PriceMonthly { get; set; }
}
