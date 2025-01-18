using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace RepeatableCallTracer.EntityFrameworkCore.Tests;

internal sealed class CallTracerDbContextFactory(SqliteConnection connection)
    : IDbContextFactory<CallTracerDbContext>
{
    public CallTracerDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<CallTracerDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new CallTracerDbContext(options);
        context.Database.EnsureCreated();

        return context;
    }
}
