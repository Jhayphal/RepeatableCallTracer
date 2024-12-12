namespace RepeatableCallTracer
{
    public interface ITracedTargetOperation : IDisposable
    {
        TParameter SetParameter<TParameter>(string name, TParameter value) where TParameter : IEquatable<TParameter>;

        TParameter SetParameter<TParameter>(string name, TParameter value, EqualityComparer<TParameter> equalityComparer);

        TParameter SetParameter<TParameter>(string name, TParameter value, Func<TParameter?, TParameter?, bool> equals);
    }
}
