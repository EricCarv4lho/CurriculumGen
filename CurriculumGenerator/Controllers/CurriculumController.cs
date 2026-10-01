using System.Security.Claims;
using CurriculumGenerator.Entities;
using CurriculumGenerator.Exceptions;
using CurriculumGenerator.Services;
using CurriculumGenerator.Validators;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CurriculumGenerator.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CurriculumController : Controller
    {
        private const long MaxPdfSizeBytes = 10 * 1024 * 1024; // 10 MB

        private readonly CurriculumService _curriculumService;
        private readonly IPdfExtractionService _pdfExtraction;
        private readonly IValidator<Curriculum> _curriculumValidator;
        private readonly GroqTranslationService _translationService;
        private readonly IAiSuggestionService _aiSuggestion;
        private readonly IUsageGuardService _usageGuard;

        public CurriculumController(
            CurriculumService curriculumService,
            IPdfExtractionService pdfExtraction,
            IValidator<Curriculum> validator,
            GroqTranslationService translationService,
            IAiSuggestionService aiSuggestion,
            IUsageGuardService usageGuard)
        {
            _curriculumService = curriculumService;
            _pdfExtraction = pdfExtraction;
            _curriculumValidator = validator;
            _translationService = translationService;
            _aiSuggestion = aiSuggestion;
            _usageGuard = usageGuard;
        }

        private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
        private string Plan => User.FindFirstValue("plan") ?? "free";

        [HttpPost("generate")]
        public IActionResult GenerateCurriculum([FromBody] Curriculum curriculum)
        {
            if (curriculum == null)
                return BadRequest("The request must not be null.");

            var validationResult = _curriculumValidator.Validate(curriculum);
            if (!validationResult.IsValid)
                return BadRequest(validationResult.Errors.Select(e => new
                {
                    Field = e.PropertyName,
                    Message = e.ErrorMessage
                }));

            try
            {
                var template = curriculum.Template ?? "classic";
                var pdfBytes = _curriculumService.GenerateCurriculum(curriculum, template);
                return File(pdfBytes, "application/pdf", "curriculum.pdf");
            }
            catch (CurriculumException ex)
            {
                return StatusCode(500, ex.Message);
            }
            catch (Exception)
            {
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpPost("translate")]
        [RequestSizeLimit(MaxPdfSizeBytes)]
        public async Task<IActionResult> TranslateCurriculum(
            IFormFile file,
            [FromForm] string targetLanguage)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded");

            if (file.Length > MaxPdfSizeBytes)
                return BadRequest("The PDF must be at most 10 MB.");

            if (file.ContentType is not ("application/pdf" or ""))
                return BadRequest("Only PDF files are accepted.");

            if (string.IsNullOrEmpty(targetLanguage))
                return BadRequest("Target language is required");

            try
            {
                // Translation is open to anonymous visitors (login is only for the
                // saved-resumes history); the usage quota is tracked per account,
                // so skip it when signed out.
                if (User.Identity?.IsAuthenticated == true)
                {
                    await _usageGuard.EnforceLimitAsync(UserId, Plan, UsageAction.Translation);
                }

                var text = _pdfExtraction.ExtractTextFromPdf(file.OpenReadStream());
                var curriculum = await _translationService.ExtractAndTranslateAsync(text, targetLanguage);

                var template = curriculum.Template ?? "classic";
                var pdfBytes = _curriculumService.GenerateCurriculum(curriculum, template, targetLanguage);

                if (User.Identity?.IsAuthenticated == true)
                {
                    await _usageGuard.RecordUsageAsync(UserId, UsageAction.Translation);
                }
                return File(pdfBytes, "application/pdf", $"translated_curriculum_{targetLanguage}.pdf");
            }
            catch (UsageLimitException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Translation failed: {ex.Message}");
            }
        }

        [HttpPost("suggest")]
        [Authorize]
        public async Task<IActionResult> SuggestImprovement([FromBody] SuggestRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Text))
                return BadRequest(new { message = "Text is required." });

            try
            {
                await _usageGuard.EnforceLimitAsync(UserId, Plan, UsageAction.AiImprove);

                var improved = await _aiSuggestion.SuggestImprovementAsync(request.Text, request.FieldType);

                await _usageGuard.RecordUsageAsync(UserId, UsageAction.AiImprove);
                return Ok(new { improved });
            }
            catch (UsageLimitException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPost("cover-letter")]
        [Authorize]
        public async Task<IActionResult> GenerateCoverLetter([FromBody] CoverLetterRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.ResumeData) || string.IsNullOrWhiteSpace(request.JobDescription))
                return BadRequest(new { message = "Resume data and job description are required." });

            try
            {
                await _usageGuard.EnforceLimitAsync(UserId, Plan, UsageAction.CoverLetter);

                var text = await _aiSuggestion.GenerateCoverLetterAsync(request.ResumeData, request.JobDescription);
                var pdfBytes = _curriculumService.GenerateCurriculumFromText(text);

                await _usageGuard.RecordUsageAsync(UserId, UsageAction.CoverLetter);
                return File(pdfBytes, "application/pdf", "cover_letter.pdf");
            }
            catch (UsageLimitException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPost("cover-letter-text")]
        [Authorize]
        public async Task<IActionResult> GenerateCoverLetterText([FromBody] CoverLetterRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.ResumeData) || string.IsNullOrWhiteSpace(request.JobDescription))
                return BadRequest(new { message = "Resume data and job description are required." });

            try
            {
                await _usageGuard.EnforceLimitAsync(UserId, Plan, UsageAction.CoverLetter);

                var text = await _aiSuggestion.GenerateCoverLetterAsync(request.ResumeData, request.JobDescription);

                await _usageGuard.RecordUsageAsync(UserId, UsageAction.CoverLetter);
                return Ok(new { text });
            }
            catch (UsageLimitException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }

    public class SuggestRequest
    {
        public string Text { get; set; } = string.Empty;
        public string FieldType { get; set; } = "objective";
    }

    public class CoverLetterRequest
    {
        public string ResumeData { get; set; } = string.Empty;
        public string JobDescription { get; set; } = string.Empty;
        public string? TargetLanguage { get; set; }
    }
}
