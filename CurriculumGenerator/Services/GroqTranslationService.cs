using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CurriculumGenerator.Entities;

namespace CurriculumGenerator.Services;

public class GroqTranslationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GroqTranslationService> _logger;
    private readonly string _model;

    public GroqTranslationService(HttpClient httpClient, IConfiguration configuration, ILogger<GroqTranslationService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _model = configuration["Groq:Model"] ?? "openai/gpt-oss-120b";
        var apiKey = configuration["Groq:ApiKey"] ?? throw new InvalidOperationException("Groq API key not configured");
        _httpClient.BaseAddress = new Uri("https://api.groq.com/openai/v1/");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
    }

    private static readonly Dictionary<string, string> LanguageMap = new()
    {
        { "en", "English" },
        { "es", "Spanish" },
        { "fr", "French" },
        { "de", "German" },
        { "it", "Italian" },
        { "pt", "Portuguese" }
    };

    public async Task<Curriculum> ExtractAndTranslateAsync(string extractedText, string languageCode)
    {
        var targetLanguageName = LanguageMap.TryGetValue(languageCode, out var name) ? name : languageCode;

        var systemPrompt = $@"You are a resume translator. Given raw text extracted from a PDF resume, you must:

1. IDENTIFY every field (name, title, city, email, phone, etc.)
2. IDENTIFY every section in the resume by its original heading
3. TRANSLATE ALL string values to {targetLanguageName} — this is MANDATORY
4. RETURN valid JSON following the exact schema below

CRITICAL RULES:
- TRANSLATE ALL text content to {targetLanguageName}. Every string value in the JSON must be in {targetLanguageName}, not the original language.
- DO NOT translate emails, phone numbers, URLs, or dates — keep them as-is
- Dates must use ISO 8601 format (e.g., ""2023-01-01T00:00:00"")
- For end dates that are ""Current"" or similar, use null
- NO markdown, ```, comments, or extra text — return ONLY the JSON object
- The ""sections"" array must contain EVERY section found in the PDF, preserving order
- Each section MUST have: ""title"" (the section heading, TRANSLATED to {targetLanguageName}) and ""content"" (the full section body, TRANSLATED to {targetLanguageName})
- Extract structured experiences, education, and languages when present

Required JSON schema (all string values must be in {targetLanguageName}):
{{
  ""fullName"": ""full name in {targetLanguageName}"",
  ""professionalTitle"": ""job title in {targetLanguageName}"",
  ""city"": ""city in {targetLanguageName}"",
  ""state"": ""state in {targetLanguageName}"",
  ""phoneNumber"": ""original phone number"",
  ""email"": ""original email"",
  ""linkedinLink"": ""original link or null"",
  ""gitHubLink"": ""original link or null"",
  ""skillSet"": [""skill 1"", ""skill 2""],
  ""experiences"": [
    {{
      ""companyName"": ""company name"",
      ""jobTitle"": ""job title"",
      ""startDate"": ""2020-01-01T00:00:00"",
      ""endDate"": null,
      ""description"": ""description in {targetLanguageName}""
    }}
  ],
  ""educationList"": [
    {{
      ""course"": ""course name in {targetLanguageName}"",
      ""institutionName"": ""institution name in {targetLanguageName}"",
      ""startDate"": ""2016-01-01T00:00:00"",
      ""endDate"": null
    }}
  ],
  ""languages"": [
    {{ ""name"": ""language in {targetLanguageName}"", ""proficiency"": ""proficiency in {targetLanguageName}"" }}
  ],
  ""sections"": [
    {{ ""title"": ""section title in {targetLanguageName}"", ""content"": ""full section content in {targetLanguageName}"" }}
  ],
  ""template"": ""classic""
}}

Fill all fields you can identify. For missing fields, use empty string. For sections, extract EVERY section exactly once with its heading translated to {targetLanguageName}.

FINAL REMINDER: You MUST translate every string value to {targetLanguageName}. Never output original-language text. If the input contains ""Minhas funções englobavam"", the output must contain ""My duties included"" (or equivalent in {targetLanguageName}), NOT the original text.""";

        var requestBody = new
        {
            model = _model,
            temperature = 0.1,
            max_tokens = 16384,
            reasoning_effort = "low",
            messages = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = extractedText }
            }
        };

        var json = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions { WriteIndented = false });
        var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.PostAsync("chat/completions", httpContent);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Groq returned {(int)response.StatusCode} ({response.StatusCode}): {errorBody}");
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseJson);
            var content = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? "";

            var cleaned = content.Trim();
            if (cleaned.StartsWith("```")) cleaned = CleanJsonResponse(cleaned);

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var curriculum = JsonSerializer.Deserialize<Curriculum>(cleaned, options) ?? new Curriculum();

            curriculum.Template ??= "classic";
            return curriculum;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao chamar a Groq para extrair/traduzir currículo");
            throw;
        }
    }

    private static string CleanJsonResponse(string raw)
    {
        var start = raw.IndexOf('{');
        var end = raw.LastIndexOf('}');
        if (start >= 0 && end > start)
            return raw[start..(end + 1)];
        return raw;
    }
}
