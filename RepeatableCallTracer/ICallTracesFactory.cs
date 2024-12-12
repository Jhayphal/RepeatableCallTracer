using System.Reflection;

namespace RepeatableCallTracer
{
    public interface ICallTracesFactory
    {
        CallTrace Create(Type targetType, MethodBase method);
    }
}
