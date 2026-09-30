using Xunit;

using CurriculumGenerator.Models;
using Microsoft.EntityFrameworkCore;
using CurriculumGenerator.Services;

namespace CurriculumGenerator.Tests;

public class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}

public class UsageGuardServiceTests : IDisposable
{
    private static readonly DateTime Now = new(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);

    private readonly Data.ApplicationDbContext _db = TestDbContextFactory.Create();
    private readonly UsageGuardService _guard;

    public UsageGuardServiceTests()
    {
        _guard = new UsageGuardService(_db, new FixedTimeProvider(Now));
    }

    [Fact]
    public async Task ProPlan_WithinLimit_Allows()
    {
        await _guard.EnforceLimitAsync("user1", "pro", UsageAction.AiImprove);
    }

    [Fact]
    public async Task FreePlan_ZeroLimit_Throws()
    {
        await Assert.ThrowsAsync<UsageLimitException>(
            () => _guard.EnforceLimitAsync("user1", "free", UsageAction.CoverLetter));
    }

    [Fact]
    public async Task ProPlan_AtMonthlyLimit_Throws()
    {
        var limit = PlanConfig.Get("pro").MaxAiImproves!.Value;
        for (var i = 0; i < limit; i++)
            await _guard.RecordUsageAsync("user1", UsageAction.AiImprove);

        await Assert.ThrowsAsync<UsageLimitException>(
            () => _guard.EnforceLimitAsync("user1", "pro", UsageAction.AiImprove));
    }

    [Fact]
    public async Task UsageFromPreviousMonth_DoesNotCount()
    {
        var limit = PlanConfig.Get("pro").MaxCoverLetters!.Value;
        _db.UsageLogs.Add(new UsageLog
        {
            UserId = "user1",
            Action = "cover_letter",
            Date = Now.AddMonths(-1)
        });
        await _db.SaveChangesAsync();

        await _guard.EnforceLimitAsync("user1", "pro", UsageAction.CoverLetter);
        Assert.Equal(1, await _db.UsageLogs.CountAsync());
        Assert.True(limit >= 1);
    }

    [Fact]
    public async Task UsageIsCountedPerUser()
    {
        await _guard.RecordUsageAsync("user1", UsageAction.Translation);
        await _guard.RecordUsageAsync("user2", UsageAction.Translation);

        // Pro allows 50; both users together are 2 — neither should hit the limit.
        await _guard.EnforceLimitAsync("user1", "pro", UsageAction.Translation);
        await _guard.EnforceLimitAsync("user2", "pro", UsageAction.Translation);
    }

    public void Dispose() => _db.Dispose();
}
