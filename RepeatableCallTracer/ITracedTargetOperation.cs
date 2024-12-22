using System;
using System.Collections.Generic;

namespace RepeatableCallTracer
{
    public interface ITracedTargetOperation : IDisposable
    {
        void SetError(string error);

        void SetError(Exception error);

        void SetParameter<TParameter>(string name, ref TParameter value) where TParameter : IEquatable<TParameter>;

        void SetParameter<TParameter>(string name, ref TParameter value, EqualityComparer<TParameter> equalityComparer);

        void SetParameter<TParameter>(string name, ref TParameter value, Func<TParameter, TParameter, bool> equals);
    }
}
