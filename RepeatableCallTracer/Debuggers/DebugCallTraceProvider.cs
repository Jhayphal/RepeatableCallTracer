namespace RepeatableCallTracer.Debuggers
{
    public sealed class DebugCallTraceProvider : IDebugCallTraceProvider
    {
        private readonly Dictionary<Type, CallTrace> traces = [];

        public bool IsDebug(Type targetType)
            => traces.ContainsKey(targetType);

        public CallTrace GetTrace(Type targetType)
            => traces[targetType];

        public void EnableDebug(Type targetType, CallTrace trace)
        {
            if (trace.AssemblyQualifiedName != targetType.AssemblyQualifiedName)
            {
                throw new ArgumentOutOfRangeException(nameof(trace));
            }

            traces[targetType] = trace;
        }

        public void DisableDebug(Type targetType)
            => traces.Remove(targetType);
    }
}
