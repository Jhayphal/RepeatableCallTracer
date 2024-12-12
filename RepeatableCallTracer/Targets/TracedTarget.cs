using System.Reflection;

using RepeatableCallTracer.Common;
using RepeatableCallTracer.Debuggers;
using RepeatableCallTracer.Dependencies;

namespace RepeatableCallTracer.Targets
{
    public abstract class TracedTarget<TTarget>(
        TTarget target,
        ICallTracesFactory callTracesFactory,
        ITracedTargetDependenciesProvider dependenciesProvider,
        ICallTraceWriter traceWriter,
        IDebugCallTraceProvider debugTraceProvider,
        CallTracerOptions options)
    {
        private readonly CallTracerSerializer serializer = new(options);
        private readonly CallTracerValidator validator = new(options);

        public TracedTarget(
            TTarget target,
            ICallTracesFactory callTracesFactory,
            ICallTraceWriter traceWriter,
            IDebugCallTraceProvider debugTraceProvider,
            CallTracerOptions options)
            : this(
                  target,
                  callTracesFactory,
                  new ReflectionBasedTracedDependenciesProvider(options),
                  traceWriter,
                  debugTraceProvider,
                  options)
        {
        }

        public TracedTarget(
            TTarget target,
            ICallTracesFactory callTracesFactory,
            ICallTraceWriter traceWriter,
            ITracedTargetDependenciesProvider dependenciesProvider,
            CallTracerOptions options)
            : this(
                  target,
                  callTracesFactory,
                  dependenciesProvider,
                  traceWriter,
                  new DebugCallTraceProvider(),
                  options)
        {
        }

        public TracedTarget(
            TTarget target,
            ICallTracesFactory callTracesFactory,
            ICallTraceWriter traceWriter,
            CallTracerOptions options)
            : this(
                  target,
                  callTracesFactory,
                  new ReflectionBasedTracedDependenciesProvider(options),
                  traceWriter,
                  new DebugCallTraceProvider(),
                  options)
        {
        }

        public TracedTarget(
            TTarget target,
            ICallTraceWriter traceWriter,
            IDebugCallTraceProvider debugTraceProvider,
            CallTracerOptions options)
            : this(
                  target,
                  new CallTracesFactory(),
                  new ReflectionBasedTracedDependenciesProvider(options),
                  traceWriter,
                  debugTraceProvider,
                  options)
        {
        }

        public TracedTarget(
            TTarget target,
            ICallTraceWriter traceWriter,
            ITracedTargetDependenciesProvider dependenciesProvider,
            CallTracerOptions options)
            : this(
                  target,
                  new CallTracesFactory(),
                  dependenciesProvider,
                  traceWriter,
                  new DebugCallTraceProvider(),
                  options)
        {
        }

        public TracedTarget(
            TTarget target,
            ICallTraceWriter traceWriter,
            CallTracerOptions options)
            : this(
                  target,
                  new CallTracesFactory(),
                  new ReflectionBasedTracedDependenciesProvider(options),
                  traceWriter,
                  new DebugCallTraceProvider(),
                  options)
        {
        }

        protected TTarget Target { get; } = target;

        protected ICallTracesFactory CallTracesFactory { get; } = callTracesFactory;

        protected ITracedTargetDependenciesProvider DependenciesProvider { get; } = dependenciesProvider;

        protected ICallTraceWriter TraceWriter { get; } = traceWriter;

        protected IDebugCallTraceProvider DebugTraceProvider { get; } = debugTraceProvider;

        private bool IsDebug
            => DebugTraceProvider.IsDebug(typeof(TTarget));

        protected ITracedTargetOperation BeginOperation(MethodBase method)
            => IsDebug
                ? BeginDebug()
                : BeginCall(method);

        private TracedTargetCallScope BeginCall(MethodBase method)
        {
            var trace = CallTracesFactory.Create(typeof(TTarget), method);

            var expectedParameters = method
                .GetParameters()
                .ToDictionary(p => p.Name!, p => p.ParameterType);
            var dependencies = DependenciesProvider.RetrieveDependenciesAndValidateIfRequired(Target);
            var scope = new TracedTargetCallScope(
                serializer,
                validator,
                TraceWriter,
                trace,
                dependencies,
                expectedParameters);

            scope.BeginOperation();

            return scope;
        }

        private TracedTargetDebugScope BeginDebug()
        {
            var trace = DebugTraceProvider.GetTrace(typeof(TTarget));
            var dependencies = DependenciesProvider.RetrieveDependenciesAndValidateIfRequired(Target);
            var scope = new TracedTargetDebugScope(trace, dependencies);
            scope.BeginDebug();

            return scope;
        }
    }
}
