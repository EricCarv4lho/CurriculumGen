using System.Security.Claims;
using CurriculumGenerator.Data;
using CurriculumGenerator.Models;
using Microsoft.EntityFrameworkCore;

namespace CurriculumGenerator.Services;

public interface IResumeService
{
    Task<IReadOnlyList<ResumeSummaryDto>> GetAllAsync(string userId);
    Task<ResumeDetailDto?> GetByIdAsync(int id, string userId);
    Task<ResumeSummaryDto?> CreateAsync(string userId, CreateResumeRequest request);
    Task<ResumeSummaryDto?> UpdateAsync(int id, string userId, CreateResumeRequest request);
    Task<bool> DeleteAsync(int id, string userId);
    Task<bool> SavePdfAsync(int id, string userId, byte[] pdfData);
    Task<(string FileName, byte[] Data)?> GetPdfAsync(int id, string userId);
}

public class ResumeService : IResumeService
{
    private const long MaxPdfSizeBytes = 10 * 1024 * 1024; // 10 MB

    private readonly ApplicationDbContext _db;

    public ResumeService(ApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<ResumeSummaryDto>> GetAllAsync(string userId)
    {
        return await _db.Resumes
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.UpdatedAt)
            .Select(r => new ResumeSummaryDto
            {
                Id = r.Id,
                Name = r.Name,
                TemplateId = r.TemplateId,
                HasPdf = r.PdfData != null,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt
            })
            .ToListAsync();
    }

    public async Task<ResumeDetailDto?> GetByIdAsync(int id, string userId)
    {
        var resume = await FindAsync(id, userId);
        if (resume == null) return null;

        return new ResumeDetailDto
        {
            Id = resume.Id,
            Name = resume.Name,
            TemplateId = resume.TemplateId,
            HasPdf = resume.PdfData != null,
            Data = System.Text.Json.JsonSerializer.Deserialize<object>(resume.Data),
            CreatedAt = resume.CreatedAt,
            UpdatedAt = resume.UpdatedAt
        };
    }

    public async Task<ResumeSummaryDto?> CreateAsync(string userId, CreateResumeRequest request)
    {
        var resume = new Resume
        {
            UserId = userId,
            Name = request.Name,
            TemplateId = request.TemplateId,
            Data = System.Text.Json.JsonSerializer.Serialize(request.Data ?? new { })
        };

        _db.Resumes.Add(resume);
        await _db.SaveChangesAsync();

        return ToSummary(resume);
    }

    public async Task<ResumeSummaryDto?> UpdateAsync(int id, string userId, CreateResumeRequest request)
    {
        var resume = await FindAsync(id, userId);
        if (resume == null) return null;

        resume.Name = request.Name;
        resume.TemplateId = request.TemplateId;
        resume.Data = System.Text.Json.JsonSerializer.Serialize(request.Data ?? new { });
        resume.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return ToSummary(resume);
    }

    public async Task<bool> DeleteAsync(int id, string userId)
    {
        var resume = await FindAsync(id, userId);
        if (resume == null) return false;

        _db.Resumes.Remove(resume);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SavePdfAsync(int id, string userId, byte[] pdfData)
    {
        var resume = await FindAsync(id, userId);
        if (resume == null) return false;

        resume.PdfData = pdfData;
        resume.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<(string FileName, byte[] Data)?> GetPdfAsync(int id, string userId)
    {
        var resume = await FindAsync(id, userId);
        if (resume?.PdfData == null) return null;

        return ($"{resume.Name}.pdf", resume.PdfData);
    }

    private Task<Resume?> FindAsync(int id, string userId) =>
        _db.Resumes.FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

    private static ResumeSummaryDto ToSummary(Resume resume) => new()
    {
        Id = resume.Id,
        Name = resume.Name,
        TemplateId = resume.TemplateId,
        HasPdf = resume.PdfData != null,
        CreatedAt = resume.CreatedAt,
        UpdatedAt = resume.UpdatedAt
    };
}
