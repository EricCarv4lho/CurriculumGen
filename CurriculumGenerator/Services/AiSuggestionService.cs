using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace CurriculumGenerator.Services;

public interface IAiSuggestionService
{
    Task<string> SuggestImprovementAsync(string text, string fieldType);
    Task<string> GenerateCoverLetterAsync(string resumeData, string jobDescription);
}

public class AiSuggestionService : IAiSuggestionService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AiSuggestionService> _logger;

    public AiSuggestionService(HttpClient httpClient, IConfiguration configuration, ILogger<AiSuggestionService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        var apiKey = configuration["Groq:ApiKey"] ?? throw new InvalidOperationException("Groq API key not configured");
        _httpClient.BaseAddress = new Uri("https://api.groq.com/openai/v1/");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
    }

    public async Task<string> SuggestImprovementAsync(string text, string fieldType)
    {
        var prompt = fieldType switch
        {
            "objective" => "Você é um especialista em currículos. Melhore o objetivo profissional abaixo para ser mais impactante, claro e atraente para recrutadores. Mantenha o mesmo sentido mas com palavras mais fortes. Retorne APENAS o texto melhorado, sem comentários, sem aspas, sem markdown.",
            "experience" => "Você é um especialista em currículos. Melhore a descrição de experiência abaixo para destacar realizações, usar verbos de ação e ser mais impactante. Use métricas quando possível. Retorne APENAS o texto melhorado, sem comentários, sem aspas, sem markdown.",
            "skill" => "Você é um especialista em currículos. Organize e melhore a lista de habilidades abaixo para ser mais profissional e atraente. Agrupe por categoria se fizer sentido. Retorne APENAS o texto melhorado, sem comentários, sem aspas, sem markdown.",
            _ => "Você é um especialista em currículos. Melhore o texto abaixo para ser mais profissional e impactante. Retorne APENAS o texto melhorado, sem comentários, sem aspas, sem markdown."
        };

        return await CallGroqAsync(prompt, text);
    }

    public async Task<string> GenerateCoverLetterAsync(string resumeData, string jobDescription)
    {
        var prompt = $@"Você é um especialista em cartas de apresentação. Crie uma carta de apresentação direta e objetiva com base nos dados do currículo e na descrição da vaga abaixo.

REGRAS:
- Saudação, 2-3 parágrafos curtos, despedida
- Seja direto: vá direto ao ponto, sem rodeios
- Destaque APENAS as experiências mais relevantes para a vaga
- Mantenha entre 100-180 palavras (máximo 180)
- NÃO use markdown, asteriscos ou formatação especial
- Retorne APENAS a carta, sem comentários

DADOS DO CURRÍCULO:
{resumeData}

DESCRIÇÃO DA VAGA:
{jobDescription}";

        return await CallGroqAsync(prompt);
    }

    private async Task<string> CallGroqAsync(string systemPrompt, string? userMessage = null)
    {
        var messages = new List<object>
        {
            new { role = "system", content = systemPrompt }
        };

        if (userMessage != null)
            messages.Add(new { role = "user", content = userMessage });

        var requestBody = new
        {
            model = "llama-3.3-70b-versatile",
            messages = messages.ToArray()
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.PostAsync("chat/completions", content);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseJson);
            return doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? "";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao chamar Groq AI");
            throw;
        }
    }
}
