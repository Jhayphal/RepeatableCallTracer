using Microsoft.Data.Sqlite;

namespace RepeatableCallTracer.EntityFrameworkCore.Tests;

public class CallTracesStorageTests
{
    [Fact]
    public void Append_Trace_Successful()
    {
        var type = this.GetType();
        var assembly = type.Assembly;
        var assemblyVersion = assembly.GetName().Version!;
        var trace = new CallTrace(assemblyVersion, type.AssemblyQualifiedName, "none", DateTime.Now);

        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var factory = new CallTracerDbContextFactory(connection);

        CallTracesStorage storage = new(factory);
        storage.Append(trace);

        var storedTraces = storage
            .GetTraces(assemblyVersion)
            .ToList();

        Assert.Single(storedTraces);
        Assert.Equal(trace, storedTraces[0]);
    }
}
