using RepeatableCallTracer.Common;
using RepeatableCallTracer.Debuggers;
using RepeatableCallTracer.Dependencies;
using RepeatableCallTracer.Targets;
using RepeatableCallTracer.Tests.Infrastructure;

namespace RepeatableCallTracer.Tests;

public partial class IndeterministicDependencyTest
{
    internal interface INumbersProvider
    {
        IEnumerable<int> GetNumbers();
    }

    internal sealed class RandomNumbersProvider : INumbersProvider
    {
        public IEnumerable<int> GetNumbers()
            => Enumerable.Range(0, 100)
                .Select(i => Random.Shared.Next(10))
                .ToList();
    }

    internal sealed class RandomNumbersProviderTracer(INumbersProvider target)
        : TracedDependency<INumbersProvider>(target, new()), INumbersProvider
    {
        public IEnumerable<int> GetNumbers()
            => IsDebuggerAttached
                ? GetResult(() => GetNumbers())
                : SetResult(() => GetNumbers(), Dependency.GetNumbers(), TracerEqualityHelper.SequenceEqual);
    }

    internal interface ISomeBusinessLogic
    {
        int Sum();
    }

    internal sealed class SomeBusinessLogic(INumbersProvider numbersProvider) : ISomeBusinessLogic
    {
        private readonly INumbersProvider numbersProvider = numbersProvider;

        public int Sum()
        {
            var firstCallResult = numbersProvider.GetNumbers();
            var secondCallResult = numbersProvider.GetNumbers();

            return firstCallResult.Union(secondCallResult).Sum();
        }
    }

    internal sealed class SomeBusinessLogicTracer(
        SomeBusinessLogic target,
        ICallTraceWriter callTraceWriter,
        IDebugCallTraceProvider debugCallTraceProvider,
        CallTracerOptions options)
        : TracedTarget<ISomeBusinessLogic>(target, callTraceWriter, debugCallTraceProvider, options), ISomeBusinessLogic
    {
        public int Sum()
        {
            var scope = BeginOperation(() => Sum());

            try
            {
                return Target.Sum();
            }
            catch (Exception ex)
            {
                scope.SetError(ex);
                throw;
            }
            finally
            {
                scope.Dispose();
            }
        }
    }

    [Fact]
    public void Test_WithoutArgs_WithIndeterministicExternalDependencies_Successful()
    {
        DebugCallTraceProvider debugCallTraceProvider = new();
        InMemoryCallTraceWriter callTraceWriter = new();

        RandomNumbersProvider numbersProvider = new();
        RandomNumbersProviderTracer numbersProviderTracer = new(numbersProvider);

        SomeBusinessLogic target = new(numbersProviderTracer);
        SomeBusinessLogicTracer tracer = new(target, callTraceWriter, debugCallTraceProvider, CallTracerOptions.Strict);

        var actualForwardCallResult = tracer.Sum();
        Assert.Single(callTraceWriter.Traces);

        var trace = callTraceWriter.Traces.First!.Value;
        debugCallTraceProvider.EnableDebug(typeof(ISomeBusinessLogic), trace);

        var actualDebugCallResult = tracer.Sum();
        Assert.Equal(actualForwardCallResult, actualDebugCallResult);
    }

    [Fact]
    public void Test_WithoutArgs_WithIndeterministicExternalDependencies_WithUntrackedDependency_ValidationException()
    {
        DebugCallTraceProvider debugCallTraceProvider = new();
        InMemoryCallTraceWriter callTraceWriter = new();
        CallTracerOptions options = new(
            ThrowIfHasUntrackedDependencies: true,
            ThrowIfParametersDifferMethodSignature: false,
            ThrowIfValueCannotBeDeserializedCorrectly: false);

        RandomNumbersProvider numbersProvider = new();

        SomeBusinessLogic target = new(numbersProvider);
        SomeBusinessLogicTracer tracer = new(target, callTraceWriter, debugCallTraceProvider, options);

        Assert.Throws<InvalidProgramException>(() => tracer.Sum());
    }

    [Fact]
    public void Test_WithoutArgs_WithIndeterministicExternalDependencies_WithUntrackedDependency_NoValidationException()
    {
        DebugCallTraceProvider debugCallTraceProvider = new();
        InMemoryCallTraceWriter callTraceWriter = new();
        CallTracerOptions options = new(
            ThrowIfHasUntrackedDependencies: false,
            ThrowIfParametersDifferMethodSignature: false,
            ThrowIfValueCannotBeDeserializedCorrectly: false);

        RandomNumbersProvider numbersProvider = new();

        SomeBusinessLogic target = new(numbersProvider);
        SomeBusinessLogicTracer tracer = new(target, callTraceWriter, debugCallTraceProvider, options);

        _ = tracer.Sum();
    }
}
