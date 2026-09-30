using System.Security.Claims;
using CurriculumGenerator.Models;
using CurriculumGenerator.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CurriculumGenerator.Controllers;

[ApiController]
[Route("api/resumes")]
[Authorize]
public class ResumesController : ControllerBase
{
    private const long MaxPdfSizeBytes = 10 * 1024 * 1024; // 10 MB

    private readonly IResumeService _resumeService;

    public ResumesController(IResumeService resumeService) => _resumeService = resumeService;

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(await _resumeService.GetAllAsync(UserId));

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var resume = await _resumeService.GetByIdAsync(id, UserId);
        return resume == null ? NotFound() : Ok(resume);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateResumeRequest request)
    {
        var resume = await _resumeService.CreateAsync(UserId, request);
        return resume == null
            ? NotFound()
            : CreatedAtAction(nameof(GetById), new { id = resume.Id }, resume);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateResumeRequest request)
    {
        var resume = await _resumeService.UpdateAsync(id, UserId, request);
        return resume == null ? NotFound() : Ok(resume);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
        => await _resumeService.DeleteAsync(id, UserId) ? NoContent() : NotFound();

    [HttpPost("{id}/pdf")]
    [RequestSizeLimit(MaxPdfSizeBytes)]
    public async Task<IActionResult> UploadPdf(int id, IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No file uploaded." });

        if (file.Length > MaxPdfSizeBytes)
            return BadRequest(new { message = "The PDF must be at most 10 MB." });

        if (file.ContentType is not ("application/pdf" or ""))
            return BadRequest(new { message = "Only PDF files are accepted." });

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        if (ms.GetBuffer().Take(4).SequenceEqual(new byte[] { 0x25, 0x50, 0x44, 0x46 }) == false)
            return BadRequest(new { message = "The uploaded file is not a valid PDF." });

        var saved = await _resumeService.SavePdfAsync(id, UserId, ms.ToArray());
        return saved
            ? Ok(new { message = "PDF uploaded successfully." })
            : NotFound();
    }

    [HttpGet("{id}/pdf")]
    public async Task<IActionResult> DownloadPdf(int id)
    {
        var pdf = await _resumeService.GetPdfAsync(id, UserId);
        return pdf == null
            ? NotFound(new { message = "No PDF available for this resume." })
            : File(pdf.Value.Data, "application/pdf", pdf.Value.FileName);
    }
}
