namespace RepeatableCallTracer.EntityFrameworkCore
{
    public sealed class DependencyMethodCall
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public required string DependencyMethodId { get; set; }

        public required int CallId { get; set; }

        public required string MethodResult { get; set; }
    }
}
