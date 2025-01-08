namespace RepeatableCallTracer.EntityFrameworkCore
{
    public sealed class TargetMethodCall
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public required string AssemblyVersion { get; set; }

        public required string AssemblyQualifiedName { get; set; }

        public required string MethodSignature { get; set; }

        public required DateTime Created { get; set; }

        public required TimeSpan Elapsed { get; set; }

        public required string Error { get; set; }
    }
}
