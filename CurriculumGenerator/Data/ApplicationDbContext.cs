using CurriculumGenerator.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CurriculumGenerator.Data;

public class ApplicationDbContext : IdentityDbContext<IdentityUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Resume> Resumes => Set<Resume>();
    public DbSet<ResumeTemplate> ResumeTemplates => Set<ResumeTemplate>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<UsageLog> UsageLogs => Set<UsageLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Resume>(e =>
        {
            e.HasIndex(r => r.UserId);
            e.Property(r => r.Data).HasColumnType("text");
            e.Property(r => r.PdfData).HasColumnType("bytea");
        });

        builder.Entity<Subscription>(e =>
        {
            e.HasIndex(s => s.UserId).IsUnique();
            e.HasIndex(s => s.MercadoPagoId);
        });

        builder.Entity<UsageLog>(e =>
        {
            e.HasIndex(l => new { l.UserId, l.Action, l.Date });
        });

        builder.Entity<ResumeTemplate>().HasData(
            new ResumeTemplate { Id = 1, Name = "Clássico", Description = "Layout tradicional e profissional", IsPremium = false, PreviewUrl = null, CreatedAt = DateTime.UtcNow },
            new ResumeTemplate { Id = 2, Name = "Moderno", Description = "Design contemporâneo com cores", IsPremium = true, PreviewUrl = null, CreatedAt = DateTime.UtcNow },
            new ResumeTemplate { Id = 3, Name = "Executivo", Description = "Sóbrio e elegante para cargos seniores", IsPremium = true, PreviewUrl = null, CreatedAt = DateTime.UtcNow },
            new ResumeTemplate { Id = 4, Name = "Criativo", Description = "Visual ousado para áreas inovadoras", IsPremium = true, PreviewUrl = null, CreatedAt = DateTime.UtcNow }
        );
    }
}
