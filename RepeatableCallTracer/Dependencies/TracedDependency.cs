using System.Reflection;

using RepeatableCallTracer.Common;

namespace RepeatableCallTracer.Dependencies
{
    public abstract class TracedDependency<TDependency>(TDependency dependency, CallTracerOptions options)
        : ITracedDependency where TDependency : notnull
    {
        private readonly CallTracerSerializer serializer = new(options);

        private CallTrace? trace;
        private int callCounter;

        public bool IsDebuggerAttached { get; private set; }

        public string AssemblyQualifiedName { get; } = typeof(TDependency).AssemblyQualifiedName!;

        protected TDependency Dependency { get; } = dependency;

        public void AttachDebugger(CallTrace trace)
        {
            this.trace = trace;
            callCounter = 0;

            IsDebuggerAttached = true;
        }

        public void DetachDebugger()
            => trace = null;

        public void BeginOperation(CallTrace trace)
        {
            this.trace = trace;
            callCounter = 0;
        }

        public void EndOperation()
        {
            trace = null;
        }

        public TResult GetResult<TResult>(MethodBase method)
        {
            ArgumentNullException.ThrowIfNull(trace);

            return trace.GetDependencyMethodResult<TResult>(this, method, ++callCounter);
        }

        public TResult SetResult<TResult>(
            MethodBase method,
            TResult methodResult) where TResult : IEquatable<TResult>
        {
            ArgumentNullException.ThrowIfNull(trace);
            ArgumentNullException.ThrowIfNull(serializer);

            var methodResultJson = serializer.SerializeAndCheckDeserializationIfRequired(
                methodResult,
                EqualityComparer<TResult>.Default.Equals);

            trace.SetDependencyMethodResult(this, method, ++callCounter, methodResultJson);

            return methodResult;
        }

        public TResult SetResult<TResult>(
            MethodBase method,
            TResult methodResult,
            IEqualityComparer<TResult> equalityComparer)
        {
            ArgumentNullException.ThrowIfNull(trace);
            ArgumentNullException.ThrowIfNull(serializer);

            var methodResultJson = serializer.SerializeAndCheckDeserializationIfRequired(
                methodResult,
                equalityComparer.Equals);

            trace.SetDependencyMethodResult(this, method, ++callCounter, methodResultJson);

            return methodResult;
        }

        public TResult SetResult<TResult>(
            MethodBase method,
            TResult methodResult,
            Func<TResult?, TResult?, bool> equals)
        {
            ArgumentNullException.ThrowIfNull(trace);
            ArgumentNullException.ThrowIfNull(serializer);

            var methodResultJson = serializer.SerializeAndCheckDeserializationIfRequired(
                methodResult,
                equals);

            trace.SetDependencyMethodResult(this, method, ++callCounter, methodResultJson);

            return methodResult;
        }
    }
}
