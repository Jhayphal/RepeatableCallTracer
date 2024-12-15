using RepeatableCallTracer.Common;
using RepeatableCallTracer.Debuggers;
using RepeatableCallTracer.Dependencies;
using RepeatableCallTracer.Targets;
using RepeatableCallTracer.Tests.Infrastructure;

namespace RepeatableCallTracer.Tests;

public partial class SerializationTest
{
    internal sealed class Employee
    {
        private readonly int age;

        public Employee() { }

        public Employee(int age)
        {
            this.age = age;
        }

        public int Age => age;
    }

    internal interface IEmployeesProvider
    {
        IEnumerable<Employee> GetEmployees();
    }

    internal sealed class EmployeesProvider : IEmployeesProvider
    {
        private readonly List<Employee> employees = Enumerable
            .Range(0, 10)
            .Select(i => new Employee(age: 20))
            .ToList();

        public IEnumerable<Employee> GetEmployees()
            => employees;
    }

    internal sealed class EmployeesProviderTracer(IEmployeesProvider target, CallTracerOptions options)
        : TracedDependency<IEmployeesProvider>(target, options), IEmployeesProvider
    {
        public IEnumerable<Employee> GetEmployees()
            => IsDebuggerAttached
                ? GetResult(() => GetEmployees())
                : SetResult(() => GetEmployees(), Dependency.GetEmployees(), TracerEqualityHelper.SequenceEqual);
    }

    internal sealed record class Car(int MaxSpeed);

    internal interface ICarsProvider
    {
        IEnumerable<Car> GetCars();
    }

    internal sealed class CarsProvider : ICarsProvider
    {
        private readonly List<Car> cars = Enumerable
            .Range(0, 10)
            .Select(i => new Car(MaxSpeed: 250))
            .ToList();

        public IEnumerable<Car> GetCars()
            => cars;
    }

    internal sealed class CarsProviderTracer(ICarsProvider target, CallTracerOptions options)
        : TracedDependency<ICarsProvider>(target, options), ICarsProvider
    {
        public IEnumerable<Car> GetCars()
            => IsDebuggerAttached
                ? GetResult(() => GetCars())
                : SetResult(() => GetCars(), Dependency.GetCars(), TracerEqualityHelper.SequenceEqual);
    }

    internal interface ISomeBusinessLogic
    {
        int AverageAge();

        int AverageSpeed();
    }

    internal sealed class SomeBusinessLogic(IEmployeesProvider employeesProvider, ICarsProvider carsProvider) : ISomeBusinessLogic
    {
        private readonly IEmployeesProvider employeesProvider = employeesProvider;
        private readonly ICarsProvider carsProvider = carsProvider;

        public int AverageAge()
            => (int)employeesProvider.GetEmployees()
                .Select(e => e.Age)
                .Average();

        public int AverageSpeed()
            => (int)carsProvider.GetCars()
                .Select(e => e.MaxSpeed)
                .Average();
    }

    internal sealed class SomeBusinessLogicTracer(
        SomeBusinessLogic target,
        ICallTraceWriter callTraceWriter,
        IDebugCallTraceProvider debugCallTraceProvider,
        CallTracerOptions options)
        : TracedTarget<ISomeBusinessLogic>(target, callTraceWriter, debugCallTraceProvider, options), ISomeBusinessLogic
    {
        public int AverageAge()
        {
            using var scope = BeginOperation(() => AverageAge());

            return Target.AverageAge();
        }

        public int AverageSpeed()
        {
            using var scope = BeginOperation(() => AverageSpeed());

            return Target.AverageSpeed();
        }
    }

    [Fact]
    public void Test_WithDeserializedResults_Failed()
    {
        DebugCallTraceProvider debugCallTraceProvider = new();
        InMemoryCallTraceWriter callTraceWriter = new();
        CallTracerOptions options = new()
        {
            ThrowIfValueCannotBeDeserializedCorrectly = true
        };

        EmployeesProvider employeesProvider = new();
        EmployeesProviderTracer employeesProviderTracer = new(employeesProvider, options);
        CarsProvider carsProvider = new();
        CarsProviderTracer carsProviderTracer = new(carsProvider, options);

        SomeBusinessLogic target = new(employeesProviderTracer, carsProviderTracer);
        SomeBusinessLogicTracer tracer = new(target, callTraceWriter, debugCallTraceProvider, options);

        Assert.Throws<ArgumentException>(() => tracer.AverageAge());
    }

    [Fact]
    public void Test_WithDeserializedResults_Success()
    {
        DebugCallTraceProvider debugCallTraceProvider = new();
        InMemoryCallTraceWriter callTraceWriter = new();
        CallTracerOptions options = new()
        {
            ThrowIfValueCannotBeDeserializedCorrectly = true
        };

        EmployeesProvider employeesProvider = new();
        EmployeesProviderTracer employeesProviderTracer = new(employeesProvider, options);
        CarsProvider carsProvider = new();
        CarsProviderTracer carsProviderTracer = new(carsProvider, options);

        SomeBusinessLogic target = new(employeesProviderTracer, carsProviderTracer);
        SomeBusinessLogicTracer tracer = new(target, callTraceWriter, debugCallTraceProvider, options);

        var actualForwardCallResult = tracer.AverageSpeed();
        Assert.Single(callTraceWriter.Traces);

        var trace = callTraceWriter.Traces.First!.Value;
        debugCallTraceProvider.EnableDebug(typeof(ISomeBusinessLogic), trace);

        var actualDebugCallResult = tracer.AverageSpeed();
        Assert.Equal(actualForwardCallResult, actualDebugCallResult);
    }
}
