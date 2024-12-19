using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

using RepeatableCallTracer.Common;

namespace RepeatableCallTracer.Dependencies
{
    public abstract class TracedDependency<TDependency> : ITracedDependency
    {
        private readonly CallTracerSerializer serializer;

        private CallTrace trace;
        private int callCounter;

        public TracedDependency(TDependency dependency, CallTracerOptions options)
        {
            serializer = new CallTracerSerializer(options);
            Dependency = dependency;
        }

        public bool IsDebuggerAttached { get; private set; }

        public string AssemblyQualifiedName { get; } = typeof(TDependency).AssemblyQualifiedName;

        protected TDependency Dependency { get; }

        public void AttachDebugger(CallTrace trace)
        {
            this.trace = trace;

            IsDebuggerAttached = true;
        }

        public void DetachDebugger()
        {
            trace = null;
            callCounter = 0;
        }

        public void BeginOperation(CallTrace trace)
        {
            this.trace = trace;
        }

        public void EndOperation()
        {
            trace = null;
            callCounter = 0;
        }

        public TResult GetResult<TResult>(Expression<Func<TResult>> expression)
        {
            if (!(expression.Body is MethodCallExpression call))
            {
                throw new ArgumentException("The expression must contain only a method call.", nameof(expression));
            }

            return GetResult<TResult>(call.Method);
        }

        private TResult GetResult<TResult>(MethodBase method)
        {
            //ArgumentNullException.ThrowIfNull(trace);

            return trace.GetDependencyMethodResult<TResult>(this, method, ++callCounter);
        }

        public TResult SetResult<TResult>(
            Expression<Func<TResult>> expression,
            TResult methodResult) where TResult : IEquatable<TResult>
        {
            if (!(expression.Body is MethodCallExpression call))
            {
                throw new ArgumentException("The expression must contain only a method call.", nameof(expression));
            }

            return SetResult(call.Method, methodResult);
        }

        public TResult SetResult<TResult>(
            Expression<Func<TResult>> expression,
            TResult methodResult,
            IEqualityComparer<TResult> equalityComparer)
        {
            if (!(expression.Body is MethodCallExpression call))
            {
                throw new ArgumentException("The expression must contain only a method call.", nameof(expression));
            }

            return SetResult(call.Method, methodResult, equalityComparer);
        }

        public TResult SetResult<TResult>(
            Expression<Func<TResult>> expression,
            TResult methodResult,
            Func<TResult, TResult, bool> equals)
        {
            if (!(expression.Body is MethodCallExpression call))
            {
                throw new ArgumentException("The expression must contain only a method call.", nameof(expression));
            }

            return SetResult(call.Method, methodResult, equals);
        }

        private TResult SetResult<TResult>(
            MethodBase method,
            TResult methodResult) where TResult : IEquatable<TResult>
        {
            //ArgumentNullException.ThrowIfNull(trace);
            //ArgumentNullException.ThrowIfNull(serializer);

            var methodResultJson = serializer.SerializeAndCheckDeserializationIfRequired(
                methodResult,
                EqualityComparer<TResult>.Default.Equals);

            trace.SetDependencyMethodResult(this, method, ++callCounter, methodResultJson);

            return methodResult;
        }

        private TResult SetResult<TResult>(
            MethodBase method,
            TResult methodResult,
            IEqualityComparer<TResult> equalityComparer)
        {
            //ArgumentNullException.ThrowIfNull(trace);
            //ArgumentNullException.ThrowIfNull(serializer);

            var methodResultJson = serializer.SerializeAndCheckDeserializationIfRequired(
                methodResult,
                equalityComparer.Equals);

            trace.SetDependencyMethodResult(this, method, ++callCounter, methodResultJson);

            return methodResult;
        }

        private TResult SetResult<TResult>(
            MethodBase method,
            TResult methodResult,
            Func<TResult, TResult, bool> equals)
        {
            //ArgumentNullException.ThrowIfNull(trace);
            //ArgumentNullException.ThrowIfNull(serializer);

            var methodResultJson = serializer.SerializeAndCheckDeserializationIfRequired(
                methodResult,
                equals);

            trace.SetDependencyMethodResult(this, method, ++callCounter, methodResultJson);

            return methodResult;
        }
    }
}
