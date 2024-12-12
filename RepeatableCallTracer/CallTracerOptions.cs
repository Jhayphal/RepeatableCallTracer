using System.Diagnostics;

namespace RepeatableCallTracer
{
    public sealed class CallTracerOptions
    {
        public bool ThrowIfHasUntrackedDependencies { get; set; } = Debugger.IsAttached;

        public bool ThrowIfParametersDifferMethodSignature { get; set; } = Debugger.IsAttached;

        public bool ThrowIfValueCannotBeDeserializedCorrectly { get; set; } = Debugger.IsAttached;
    }
}
