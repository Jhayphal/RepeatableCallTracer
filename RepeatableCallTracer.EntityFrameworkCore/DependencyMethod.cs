namespace RepeatableCallTracer.EntityFrameworkCore
{
    public sealed class DependencyMethod
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public required string TargetMethodCallId { get; set; }

        public required string DependencyKey { get; set; }

        public required string MethodSignature { get; set; }
    }
}
