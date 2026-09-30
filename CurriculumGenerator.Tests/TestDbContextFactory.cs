using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CurriculumGenerator.Tests;

public static class TestDbContextFactory
{
    public static Data.ApplicationDbContext Create()
    {
        var options = new DbContextOptionsBuilder<Data.ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new Data.ApplicationDbContext(options);
    }
}
