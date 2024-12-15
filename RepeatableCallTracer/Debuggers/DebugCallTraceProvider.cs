using System.Reflection;

namespace RepeatableCallTracer.Debuggers
{
    public sealed class DebugCallTraceProvider : IDebugCallTraceProvider
    {
        private readonly Dictionary<Type, Dictionary<string, CallTrace>> traces = [];

        public bool IsDebug(Type targetType, MethodBase method)
            => traces.TryGetValue(targetType, out var calls) && calls.ContainsKey(method.ToString()!);

        public CallTrace GetTrace(Type targetType, MethodBase method)
            => traces[targetType][method.ToString()!];

        public void EnableDebug(Type targetType, CallTrace trace)
        {
            if (trace.AssemblyQualifiedName != targetType.AssemblyQualifiedName)
            {
                throw new ArgumentOutOfRangeException(nameof(trace));
            }

            if (!traces.TryGetValue(targetType, out var calls))
            {
                calls = [];

                traces[targetType] = calls;
            }

            calls[trace.MethodSignature] = trace;
        }

        public bool DisableDebug(Type targetType)
            => traces.Remove(targetType);

        public bool DisableDebug(Type targetType, MethodBase method)
        {
            if (!traces.TryGetValue(targetType, out var calls))
            {
                return false;
            }

            return calls.Remove(method.ToString()!);
        }
    }
}
