using CurriculumGenerator.Data;
using CurriculumGenerator.Models;
using Microsoft.EntityFrameworkCore;

namespace CurriculumGenerator.Services;

public enum UsageAction
{
    Translation,
    AiImprove,
    CoverLetter
}

public class UsageLimitException : Exception
{
    public UsageLimitException(string message) : base(message) { }
}

/// <summary>
/// Enforces the monthly AI usage limits defined in PlanConfig and records usage in UsageLogs.
/// </summary>
public interface IUsageGuardService
{
    /// <exception cref="UsageLimitException">Thrown when the plan limit for the action was reached.</exception>
    Task EnforceLimitAsync(string userId, string plan, UsageAction action);

    Task RecordUsageAsync(string userId, UsageAction action);
}

public class UsageGuardService : IUsageGuardService
{
    private readonly ApplicationDbContext _db;
    private readonly TimeProvider _timeProvider;

    public UsageGuardService(ApplicationDbContext db, TimeProvider? timeProvider = null)
    {
        _db = db;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task EnforceLimitAsync(string userId, string plan, UsageAction action)
    {
        var definition = PlanConfig.Get(plan);
        var limit = GetLimit(definition, action);

        if (limit is null) return; // unlimited
        if (limit <= 0)
            throw new UsageLimitException(
                $"Este recurso não está disponível no plano {definition.Name}. Faça upgrade para continuar usando.");

        var used = await CountUsageAsync(userId, action);
        if (used >= limit)
            throw new UsageLimitException(
                $"Você atingiu o limite mensal de {limit} uso(s) do plano {definition.Name} para este recurso.");
    }

    public async Task RecordUsageAsync(string userId, UsageAction action)
    {
        _db.UsageLogs.Add(new UsageLog
        {
            UserId = userId,
            Action = ActionName(action),
            Date = _timeProvider.GetUtcNow().UtcDateTime
        });
        await _db.SaveChangesAsync();
    }

    private Task<int> CountUsageAsync(string userId, UsageAction action)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var nextMonthStart = monthStart.AddMonths(1);

        return _db.UsageLogs
            .Where(l => l.UserId == userId
                        && l.Action == ActionName(action)
                        && l.Date >= monthStart
                        && l.Date < nextMonthStart)
            .CountAsync();
    }

    private static int? GetLimit(PlanDefinition definition, UsageAction action) => action switch
    {
        UsageAction.Translation => definition.MaxTranslations,
        UsageAction.AiImprove => definition.MaxAiImproves,
        UsageAction.CoverLetter => definition.MaxCoverLetters,
        _ => 0
    };

    private static string ActionName(UsageAction action) => action switch
    {
        UsageAction.Translation => "translation",
        UsageAction.AiImprove => "ai_improve",
        UsageAction.CoverLetter => "cover_letter",
        _ => action.ToString()
    };
}
