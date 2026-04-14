using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.ExceptionServices;

namespace Mapify.NET;

public partial class Mapify {
    private Expression<Func<TSource, TTarget>> GetRequiredRuntimeMap<TSource, TTarget>(
        string? name,
        IReadOnlyDictionary<string, object?> parameters
    ) {
        if (TryCreateRuntimeMapExpression<TSource, TTarget>(
                (sourceType, targetType, requestedMapName) => ResolveRuntimeMapCandidate(sourceType, targetType, requestedMapName ?? name, parameters),
                name,
                out var runtimeMapExpression)) {
            return runtimeMapExpression;
        }

        if (name == null && _useDefaultMapIfTypeMapIsMissing) {
            var defaultCacheKey = new Tuple<Type, Type>(typeof(TSource), typeof(TTarget));
            if (_defaultMapCache.TryGetValue(defaultCacheKey, out var existingDefaultMap)) {
                return ApplyParameters((Expression<Func<TSource, TTarget>>)existingDefaultMap, parameters);
            }

            var defaultMap = CreateMap<TSource, TTarget>(null, null, (sourceType, targetType, requestedMapName) => ResolveExistingMapForBuild(sourceType, targetType, requestedMapName));
            _defaultMapCache[defaultCacheKey] = defaultMap;
            return ApplyParameters(defaultMap, parameters);
        }

        if (name == null) {
            throw new ArgumentException($"Missing type map configuration for TSource ({typeof(TSource).FullName}) to TTarget ({typeof(TTarget).FullName})");
        }

        throw new ArgumentException($"Missing named type map configuration '{name}' for TSource ({typeof(TSource).FullName}) to TTarget ({typeof(TTarget).FullName})");
    }

    internal LambdaExpression GetRequiredRuntimeMapUntyped(
        Type sourceType,
        Type targetType,
        string? name,
        IReadOnlyDictionary<string, object?>? parameters
    ) {
        var genericMethod = typeof(Mapify)
            .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single(m => m.Name == nameof(GetRequiredRuntimeMap)
                && m.IsGenericMethodDefinition
                && m.GetGenericArguments().Length == 2
                && m.GetParameters().Length == 2)
            .MakeGenericMethod(sourceType, targetType);

        var runtimeParameters = parameters ?? _emptyParameters;
        try {
            return (LambdaExpression)genericMethod.Invoke(this, [name, runtimeParameters])!;
        } catch (TargetInvocationException ex) when (ex.InnerException != null) {
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            throw;
        }
    }

    private LambdaExpression? ResolveRuntimeMapCandidate(
        Type sourceType,
        Type targetType,
        string? name,
        IReadOnlyDictionary<string, object?> parameters
    ) {
        var key = new MapKey(sourceType, targetType, name);
        if (_converters.TryGetValue(key, out var existingConverter)) {
            return ApplyParameters(existingConverter, parameters);
        }

        return null;
    }
}
