using System;
using System.Collections.Generic;
using System.Linq;

namespace RepeatableCallTracer.Targets
{
    internal sealed class TracedTargetDebugScope : ITracedTargetOperation
    {
        private readonly CallTrace trace;
        private readonly IEnumerable<ITracedDependency> dependencies;

        public TracedTargetDebugScope(
            CallTrace trace,
            IEnumerable<ITracedDependency> dependencies)
        {
            this.trace = trace;
            this.dependencies = dependencies;

            foreach (var operation in dependencies)
            {
                operation.AttachDebugger(trace);
            }
        }

        public void Dispose()
        {
            foreach (var operation in dependencies.Reverse())
            {
                operation.DetachDebugger();
            }
        }

        public void SetParameter<TParameter>(string name, ref TParameter value) where TParameter : IEquatable<TParameter>
            => SetParameter(name, ref value, EqualityComparer<TParameter>.Default.Equals);

        public void SetParameter<TParameter>(string name, ref TParameter value, EqualityComparer<TParameter> equalityComparer)
            => SetParameter(name, ref value, EqualityComparer<TParameter>.Default.Equals);

        public void SetParameter<TParameter>(string name, ref TParameter value, Func<TParameter, TParameter, bool> equals)
            => value = trace.GetTargetMethodParameter<TParameter>(name);
    }
}
