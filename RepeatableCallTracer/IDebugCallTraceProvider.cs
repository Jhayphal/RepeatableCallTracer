using System;
using System.Reflection;

namespace RepeatableCallTracer
{
    public interface IDebugCallTraceProvider
    {
        bool IsDebug(Type targetType, MethodBase method);

        CallTrace GetTrace(Type targetType, MethodBase method);
    }
}
