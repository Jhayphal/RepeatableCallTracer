using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using RepeatableCallTracer.Debuggers;
using RepeatableCallTracer.Dependencies;

namespace RepeatableCallTracer.Targets
{
    public abstract class TracedTarget<TTarget>
    {
        private readonly Type targetType = typeof(TTarget);
        private readonly CallTracerOptions options;

        public TracedTarget(
            TTarget target,
            ITracedTargetDependenciesProvider dependenciesProvider,
            ICallTraceWriter traceWriter,
            IDebugCallTraceProvider debugTraceProvider,
            CallTracerOptions options)
        {
            this.options = options;
            Target = target;
            DependenciesProvider = dependenciesProvider;
            TraceWriter = traceWriter;
            DebugTraceProvider = debugTraceProvider;
        }

        public TracedTarget(
            TTarget target,
            ICallTraceWriter traceWriter,
            IDebugCallTraceProvider debugTraceProvider,
            CallTracerOptions options)
            : this(
                  target,
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
                  new ReflectionBasedTracedDependenciesProvider(options),
                  traceWriter,
                  new DebugCallTraceProvider(),
                  options)
        {
        }

        protected TTarget Target { get; }

        protected ITracedTargetDependenciesProvider DependenciesProvider { get; }

        protected ICallTraceWriter TraceWriter { get; }

        protected IDebugCallTraceProvider DebugTraceProvider { get; }

        private bool IsDebug(MethodBase method)
            => DebugTraceProvider.IsDebug(targetType, method);

        protected ITracedTargetOperation BeginOperation(Expression<Action> expression)
        {
            if (!(expression.Body is MethodCallExpression call))
            {
                throw new ArgumentException("The expression must contain only a method call.", nameof(expression));
            }

            return BeginOperation(call.Method);
        }

        protected ITracedTargetOperation BeginOperation<TResult>(Expression<Func<TResult>> expression)
        {
            if (!(expression.Body is MethodCallExpression call))
            {
                throw new ArgumentException("The expression must contain only a method call.", nameof(expression));
            }

            return BeginOperation(call.Method);
        }

        private ITracedTargetOperation BeginOperation(MethodBase method)
            => IsDebug(method)
                ? BeginDebug(method)
                : (ITracedTargetOperation)BeginCall(method);

        private TracedTargetCallScope BeginCall(MethodBase method)
        {
            var methodSignature = method.ToString();
            if (string.IsNullOrWhiteSpace(methodSignature))
            {
                throw new ArgumentException("Method signature cannot be empty.", nameof(methodSignature));
            }

            var expectedParameters = method
                .GetParameters()
                .ToDictionary(p => p.Name, p => p.ParameterType);

            var assemblyVersion = targetType.Assembly.GetName().Version;
            if (assemblyVersion is null)
            {
                throw new ArgumentNullException(nameof(assemblyVersion));
            }

            var trace = new CallTrace(assemblyVersion, targetType.AssemblyQualifiedName, methodSignature, DateTime.UtcNow);

            var dependencies = DependenciesProvider.RetrieveDependenciesAndValidateIfRequired(Target);
            
            return new TracedTargetCallScope(
                trace,
                options,
                expectedParameters,
                dependencies,
                TraceWriter);
        }

        private TracedTargetDebugScope BeginDebug(MethodBase method)
        {
            var trace = DebugTraceProvider.GetTrace(targetType, method);
            var dependencies = DependenciesProvider.RetrieveDependenciesAndValidateIfRequired(Target);
            
            return new TracedTargetDebugScope(trace, dependencies);
        }
    }
}
