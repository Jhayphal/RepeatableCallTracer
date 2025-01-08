namespace RepeatableCallTracer.EntityFrameworkCore
{
    public sealed class TargetMethodParameter
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public required string TargetMethodCallId { get; set; }

        public required string Name { get; set; }

        public required string Value { get; set; }
    }
}
