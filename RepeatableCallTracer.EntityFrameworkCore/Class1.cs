using Microsoft.EntityFrameworkCore;

namespace RepeatableCallTracer.EntityFrameworkCore
{
    public sealed class TargetMethodCall
    {
        public TargetMethodCall()
        {
            Id = Guid.NewGuid().ToString();
        }

        public required string Id { get; set; }

        public required string AssemblyVersion { get; set; }

        public required string AssemblyQualifiedName { get; set; }

        public required string MethodSignature { get; set; }

        public required DateTime Created { get; set; }

        public required TimeSpan Elapsed { get; set; }

        public required string Error { get; set; }
    }

    public sealed class TargetMethodParameter
    {
        public TargetMethodParameter()
        {
            Id = Guid.NewGuid().ToString();
        }

        public required string Id { get; set; }

        public required string TargetMethodCallId { get; set; }

        public required string Name { get; set; }

        public required string Value { get; set; }
    }

    public sealed class DependencyMethod
    {
        public DependencyMethod()
        {
            Id = Guid.NewGuid().ToString();
        }

        public required string Id { get; set; }

        public required string TargetMethodCallId { get; set; }

        public required string DependencyKey { get; set; }

        public required string MethodSignature { get; set; }
    }

    public sealed class DependencyMethodCall
    {
        public DependencyMethodCall()
        {
            Id = Guid.NewGuid().ToString();
        }

        public required string Id { get; set; }

        public required string DependencyMethodId { get; set; }

        public required int CallId { get; set; }

        public required string MethodResult { get; set; }
    }

    public sealed class CallTracerDbContext(DbContextOptions<CallTracerDbContext> options)
        : DbContext(options)
    {
        public DbSet<TargetMethodCall> Traces { get; set; }

        public DbSet<TargetMethodParameter> Parameters { get; set; }

        public DbSet<DependencyMethod> Dependencies { get; set; }

        public string SchemaName { get; } = "CallTracer";

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TargetMethodCall>(b =>
            {
                b.HasKey(t => t.Id);
                b.HasIndex(t => new { t.AssemblyQualifiedName, t.MethodSignature, t.Created }).IsUnique();
                b.HasIndex(t => new { t.AssemblyVersion, t.Created });
                b.ToTable("TargetMethodCall", SchemaName);

                b.HasMany<TargetMethodParameter>().WithOne().HasForeignKey(p => p.TargetMethodCallId).IsRequired();
                b.HasMany<DependencyMethod>().WithOne().HasForeignKey(d => d.TargetMethodCallId).IsRequired();
            });

            modelBuilder.Entity<TargetMethodParameter>(b =>
            {
                b.HasKey(t => t.Id);
                b.ToTable("TargetMethodParameter", SchemaName);
            });

            modelBuilder.Entity<DependencyMethod>(b =>
            {
                b.HasKey(t => t.Id);

                b.HasMany<DependencyMethodCall>().WithOne().HasForeignKey(p => p.DependencyMethodId).IsRequired();
                
                b.ToTable("DependencyMethod", SchemaName);
            });

            modelBuilder.Entity<DependencyMethodCall>(b =>
            {
                b.HasKey(t => t.Id);
                b.ToTable("DependencyMethodCall", SchemaName);
            });
        }
    }

    public sealed class DatabaseCallTraceWriter : ICallTraceWriter
    {
        public void Append(CallTrace trace)
        {
            throw new NotImplementedException();
        }
    }
}
