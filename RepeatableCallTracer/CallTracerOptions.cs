using System.Diagnostics;

namespace RepeatableCallTracer
{
    public readonly record struct CallTracerOptions(
        bool ThrowIfHasUntrackedDependencies,
        bool ThrowIfParametersDifferMethodSignature,
        bool ThrowIfValueCannotBeDeserializedCorrectly)
    {
        public static CallTracerOptions Default
        {
            get
            {
                var isDebuggerAttached = Debugger.IsAttached;

                return new(isDebuggerAttached, isDebuggerAttached, isDebuggerAttached);
            }
        }

        public static CallTracerOptions Strict => new(true, true, true);

        public static CallTracerOptions Fastest => new(false, false, false);
    }
}
