using System.Linq.Expressions;

namespace Mapify.NET;

public partial class Mapify {
    private void AddMapUntyped(LambdaExpression mappingExpression, string? name) {
        var sourceType = mappingExpression.Parameters[0].Type;
        var targetType = mappingExpression.ReturnType;

        var method = typeof(Mapify).GetMethod(nameof(AddMapGeneric), System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        var generic = method.MakeGenericMethod(sourceType, targetType);
        generic.Invoke(this, [mappingExpression, name]);
    }

    private void SetMapUntyped(LambdaExpression mappingExpression, string? name, bool compileCaches) {
        var sourceType = mappingExpression.Parameters[0].Type;
        var targetType = mappingExpression.ReturnType;

        var method = typeof(Mapify).GetMethod(nameof(SetMapGeneric), System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        var generic = method.MakeGenericMethod(sourceType, targetType);
        generic.Invoke(this, [mappingExpression, name, compileCaches]);
    }

    private void AddMapGeneric<TSource, TTarget>(LambdaExpression mappingExpression, string? name)
        => AddMap((Expression<Func<TSource, TTarget>>)mappingExpression, name);

    private void SetMapGeneric<TSource, TTarget>(LambdaExpression mappingExpression, string? name, bool compileCaches) {
        var key = new MapKey(typeof(TSource), typeof(TTarget), name);
        var expression = (Expression<Func<TSource, TTarget>>)mappingExpression;

        _converters[key] = expression;
        ClearRuntimeSourceBaseFallbackCaches();

        if (!compileCaches) {
            _compiledMapToExistingCache.Remove(key);
            _compiledMapToNewCache.Remove(key);
            return;
        }

        UpdateCompiledMapCaches(key, expression, clearExisting: true);
    }

    private void AddMap<TSource, TTarget>(Expression<Func<TSource, TTarget>> mappingExpression, string? name = null) {
        var key = new MapKey(typeof(TSource), typeof(TTarget), name);
        if (_converters.ContainsKey(key)) {
            var mappingScope = name == null ? "default" : $"named '{name}'";
            throw new ArgumentException($"There already exists a {mappingScope} mapping from TSource ({typeof(TSource).FullName}) to TTarget ({typeof(TTarget).FullName}). There can only be one mapping per name and source/target combination.");
        }

        _converters[key] = mappingExpression;
        ClearRuntimeSourceBaseFallbackCaches();
        UpdateCompiledMapCaches(key, mappingExpression, clearExisting: false);
    }

    private void ClearRuntimeSourceBaseFallbackCaches() {
        _runtimeSourceBaseFallbackCache.Clear();
        _runtimeSourceBaseFallbackMissCache.Clear();
    }

    private void UpdateCompiledMapCaches<TSource, TTarget>(
        MapKey key,
        Expression<Func<TSource, TTarget>> mappingExpression,
        bool clearExisting
    ) {
        if (clearExisting) {
            _compiledMapToExistingCache.Remove(key);
            _compiledMapToNewCache.Remove(key);
        }

        if (ContainsParameterMarkers(mappingExpression)) {
            return;
        }

        _compiledMapToNewCache[key] = mappingExpression.Compile();

        if (mappingExpression.Body is MemberInitExpression) {
            _compiledMapToExistingCache[key] = CompileMapper(mappingExpression);
        }
    }
}
