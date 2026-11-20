using System.Reflection;

namespace AuroraMachine2SG.Tests;

[AttributeUsage(AttributeTargets.Method)]
public sealed class FactAttribute : Attribute { }

public sealed class AssertionException(string message) : Exception(message);

public static class AssertEx
{
    public static void True(bool condition, string message = "Expected true but got false.")
    {
        if (!condition) throw new AssertionException(message);
    }

    public static void False(bool condition, string message = "Expected false but got true.")
    {
        if (condition) throw new AssertionException(message);
    }

    public static void Equal<T>(T expected, T actual, string? label = null)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new AssertionException($"{label ?? "Assert.Equal"} failed: expected '{expected}', got '{actual}'.");
    }

    public static void Near(double expected, double actual, double tolerance = 1e-4, string? label = null)
    {
        if (double.IsNaN(expected) && double.IsNaN(actual)) return;
        if (Math.Abs(expected - actual) > tolerance)
            throw new AssertionException($"{label ?? "Assert.Near"} failed: expected {expected:F6} ± {tolerance}, got {actual:F6}.");
    }

    public static void NotNull(object? obj, string? label = null)
    {
        if (obj is null)
            throw new AssertionException($"{label ?? "Assert.NotNull"} failed: value was null.");
    }
}

public static class Program
{
    public static int Main(string[] args)
    {
        var filter = args.FirstOrDefault();
        var methods = Assembly.GetExecutingAssembly()
            .GetTypes()
            .OrderBy(t => t.FullName)
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                .Where(m => m.GetCustomAttribute<FactAttribute>() is not null)
                .OrderBy(m => m.Name)
                .Select(m => (Type: t, Method: m)))
            .Where(x => string.IsNullOrEmpty(filter) || $"{x.Type.Name}.{x.Method.Name}".Contains(filter, StringComparison.OrdinalIgnoreCase))
            .ToList();

        int passed = 0, failed = 0;
        foreach (var (type, method) in methods)
        {
            var name = $"{type.Name}.{method.Name}";
            try
            {
                var instance = method.IsStatic ? null : Activator.CreateInstance(type);
                var result = method.Invoke(instance, null);
                if (result is Task task) task.GetAwaiter().GetResult();
                Console.WriteLine($"  [PASS] {name}");
                passed++;
            }
            catch (TargetInvocationException tie)
            {
                var inner = tie.InnerException ?? tie;
                Console.WriteLine($"  [FAIL] {name}: {inner.GetType().Name} - {inner.Message}");
                failed++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  [FAIL] {name}: {ex.GetType().Name} - {ex.Message}");
                failed++;
            }
        }

        Console.WriteLine($"\nSummary: {passed} passed, {failed} failed, {methods.Count} total.");
        if (failed > 0) return 1;
        Console.WriteLine("ALL TESTS PASSED");
        return 0;
    }
}
