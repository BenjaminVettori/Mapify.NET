using System.Linq.Expressions;

namespace Mapify.NET;

public partial class Mapify {
    private const string _runtimeExpressionDebugEnvVar = "MAPIFY_DEBUG_RUNTIME_EXPRESSION";

    private static bool IsRuntimeExpressionDebugEnabled() {
        var value = Environment.GetEnvironmentVariable(_runtimeExpressionDebugEnvVar);
        return string.Equals(value, "1", StringComparison.Ordinal)
            || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase);
    }

    private static void LogRuntimeExpression<TSource, TTarget>(
        string? name,
        Expression<Func<TSource, TTarget>> expression,
        bool fromCache
    ) {
        if (!IsRuntimeExpressionDebugEnabled()) {
            return;
        }

        var mapName = name ?? "<default>";
        Console.WriteLine($"[Mapify Runtime Expression] fromCache={fromCache} source={typeof(TSource).FullName} target={typeof(TTarget).FullName} name={mapName}");
        Console.WriteLine(expression.ToString());
    }
}
