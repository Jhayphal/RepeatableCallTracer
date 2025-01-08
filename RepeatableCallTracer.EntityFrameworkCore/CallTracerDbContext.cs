using Microsoft.EntityFrameworkCore;

namespace RepeatableCallTracer.EntityFrameworkCore
{
    public sealed class CallTracerDbContext(DbContextOptions<CallTracerDbContext> options)
        : DbContext(options)
    {
        public DbSet<TargetMethodCall> Traces { get; set; }

        public DbSet<TargetMethodParameter> Parameters { get; set; }

        public DbSet<DependencyMethod> Dependencies { get; set; }

        public DbSet<DependencyMethodCall> DependencyMethodCalls { get; set; }

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
}
