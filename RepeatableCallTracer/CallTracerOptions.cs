using System.Diagnostics;

namespace RepeatableCallTracer
{
    public readonly struct CallTracerOptions
    {
        public CallTracerOptions(
            bool ThrowIfHasUntrackedDependencies,
            bool ThrowIfParametersDifferMethodSignature,
            bool ThrowIfValueCannotBeDeserializedCorrectly)
        {
            this.ThrowIfHasUntrackedDependencies = ThrowIfHasUntrackedDependencies;
            this.ThrowIfParametersDifferMethodSignature = ThrowIfParametersDifferMethodSignature;
            this.ThrowIfValueCannotBeDeserializedCorrectly = ThrowIfValueCannotBeDeserializedCorrectly;
        }

        public static CallTracerOptions Default
        {
            get
            {
                var isDebuggerAttached = Debugger.IsAttached;

                return new CallTracerOptions(isDebuggerAttached, isDebuggerAttached, isDebuggerAttached);
            }
        }

        public static CallTracerOptions Strict => new CallTracerOptions(true, true, true);

        public static CallTracerOptions Fastest => new CallTracerOptions(false, false, false);

        public bool ThrowIfHasUntrackedDependencies { get; }

        public bool ThrowIfParametersDifferMethodSignature { get; }

        public bool ThrowIfValueCannotBeDeserializedCorrectly { get; }
    }
}
