using System.Reflection;
using System.Runtime.CompilerServices;

using RepeatableCallTracer.Common;
using RepeatableCallTracer.Debuggers;
using RepeatableCallTracer.Dependencies;
using RepeatableCallTracer.Targets;

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
        [MethodImpl(MethodImplOptions.NoInlining)]
        public IEnumerable<int> GetNumbers()
        {
            var method = MethodBase.GetCurrentMethod()!;

            return IsDebuggerAttached
                ? GetResult<IEnumerable<int>>(method)
                : SetResult(method, Dependency.GetNumbers(), TracerEqualityHelper.SequenceEqual);
        }
    }

    internal interface ISumBusinessLogic
    {
        int Sum();
    }

    internal sealed class SumBusinessLogic(INumbersProvider numbersProvider) : ISumBusinessLogic
    {
        private readonly INumbersProvider numbersProvider = numbersProvider;

        public int Sum()
        {
            var firstCallResult = numbersProvider.GetNumbers();
            var secondCallResult = numbersProvider.GetNumbers();

            return firstCallResult.Union(secondCallResult).Sum();
        }
    }

    internal sealed class SumBusinessLogicTracer(
        SumBusinessLogic target,
        ICallTraceWriter callTraceWriter,
        IDebugCallTraceProvider debugCallTraceProvider,
        CallTracerOptions options)
        : TracedTarget<ISumBusinessLogic>(target, callTraceWriter, debugCallTraceProvider, options), ISumBusinessLogic
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public int Sum()
        {
            using var scope = BeginOperation(MethodBase.GetCurrentMethod()!);

            return Target.Sum();
        }
    }

    [Fact]
    public void Test_WithoutArgs_WithIndeterministicExternalDependencies_Successful()
    {
        DebugCallTraceProvider debugCallTraceProvider = new();
        InMemoryCallTraceWriter callTraceWriter = new();
        CallTracerOptions options = new();

        RandomNumbersProvider numbersProvider = new();
        RandomNumbersProviderTracer numbersProviderTracer = new(numbersProvider);

        SumBusinessLogic target = new(numbersProviderTracer);
        SumBusinessLogicTracer tracer = new(target, callTraceWriter, debugCallTraceProvider, options);

        var actualForwardCallResult = tracer.Sum();
        Assert.Single(callTraceWriter.Traces);

        var trace = callTraceWriter.Traces.First!.Value;
        debugCallTraceProvider.EnableDebug(typeof(ISumBusinessLogic), trace);

        var actualDebugCallResult = tracer.Sum();
        Assert.Equal(actualForwardCallResult, actualDebugCallResult);
    }

    [Fact]
    public void Test_WithoutArgs_WithIndeterministicExternalDependencies_WithUntrackedDependency_ValidationException()
    {
        DebugCallTraceProvider debugCallTraceProvider = new();
        InMemoryCallTraceWriter callTraceWriter = new();
        CallTracerOptions options = new()
        {
            ThrowIfHasUntrackedDependencies = true
        };

        RandomNumbersProvider numbersProvider = new();

        SumBusinessLogic target = new(numbersProvider);
        SumBusinessLogicTracer tracer = new(target, callTraceWriter, debugCallTraceProvider, options);

        Assert.Throws<InvalidProgramException>(() => tracer.Sum());
    }

    [Fact]
    public void Test_WithoutArgs_WithIndeterministicExternalDependencies_WithUntrackedDependency_NoValidationException()
    {
        DebugCallTraceProvider debugCallTraceProvider = new();
        InMemoryCallTraceWriter callTraceWriter = new();
        CallTracerOptions options = new()
        {
            ThrowIfHasUntrackedDependencies = false
        };

        RandomNumbersProvider numbersProvider = new();

        SumBusinessLogic target = new(numbersProvider);
        SumBusinessLogicTracer tracer = new(target, callTraceWriter, debugCallTraceProvider, options);

        _ = tracer.Sum();
    }
}
