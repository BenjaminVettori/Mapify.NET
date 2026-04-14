using System.Linq.Expressions;

namespace Mapify.NET;

public partial class Mapify {
    /// <summary>
    /// Maps values from the source object to an existing target object using a named map.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TTarget">The target type.</typeparam>
    /// <param name="source">The source object.</param>
    /// <param name="target">The target object to update.</param>
    /// <param name="name">The map name.</param>
    /// <exception cref="ArgumentException">Thrown when the name is invalid or the named map is missing.</exception>
    /// <exception cref="NotSupportedException">Thrown when the map cannot target an existing instance.</exception>
    public void Map<TSource, TTarget>(TSource source, TTarget target, string name) {
        if (string.IsNullOrWhiteSpace(name)) {
            throw new ArgumentException("Mapping name must not be null or whitespace.", nameof(name));
        }

        var key = new MapKey(typeof(TSource), typeof(TTarget), name);
        if (_compiledMapToExistingCache.TryGetValue(key, out var map)) {
            ((Action<TSource, TTarget>)map).Invoke(source, target);
            return;
        }

        var expression = GetRequiredRuntimeMap<TSource, TTarget>(name, _emptyParameters);

        if (expression.Body is not MemberInitExpression) {
            throw new NotSupportedException($"Mapping from TSource ({typeof(TSource).FullName}) to TTarget ({typeof(TTarget).FullName}) cannot map to an existing target instance because the map does not use an object initializer (x => new TTarget {{ ... }}). Use Map(source, name) instead.");
        }

        var compiled = CompileMapper(expression);
        _compiledMapToExistingCache[key] = compiled;
        compiled.Invoke(source, target);
    }

    /// <summary>
    /// Maps values from the source object to an existing target object using a named map and runtime parameters.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TTarget">The target type.</typeparam>
    /// <param name="source">The source object.</param>
    /// <param name="target">The target object to update.</param>
    /// <param name="name">The map name.</param>
    /// <param name="parameters">Runtime parameters used by <see cref="MapifyProfile"/> <c>Parameter&lt;T&gt;(name)</c> markers.</param>
    /// <exception cref="ArgumentException">Thrown when the name is invalid or the named map is missing.</exception>
    /// <exception cref="NotSupportedException">Thrown when the map cannot target an existing instance.</exception>
    public void Map<TSource, TTarget>(TSource source, TTarget target, string name, IReadOnlyDictionary<string, object?> parameters) {
        if (string.IsNullOrWhiteSpace(name)) {
            throw new ArgumentException("Mapping name must not be null or whitespace.", nameof(name));
        }

        ValidateRuntimeParameters(parameters);

        var expression = GetRequiredRuntimeMap<TSource, TTarget>(name, parameters);

        if (expression.Body is not MemberInitExpression) {
            throw new NotSupportedException($"Mapping from TSource ({typeof(TSource).FullName}) to TTarget ({typeof(TTarget).FullName}) cannot map to an existing target instance because the map does not use an object initializer (x => new TTarget {{ ... }}). Use Map(source, name, parameters) instead.");
        }

        var compiled = CompileMapper(expression);
        compiled.Invoke(source, target);
    }

    /// <summary>
    /// Maps values from the source object to an existing target object using the default map.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TTarget">The target type.</typeparam>
    /// <param name="source">The source object.</param>
    /// <param name="target">The target object to update.</param>
    /// <exception cref="ArgumentException">Thrown when no map is available.</exception>
    /// <exception cref="NotSupportedException">Thrown when the map cannot target an existing instance.</exception>
    public void Map<TSource, TTarget>(TSource source, TTarget target) {
        var key = new MapKey(typeof(TSource), typeof(TTarget), null);
        if (_compiledMapToExistingCache.TryGetValue(key, out var map)) {
            ((Action<TSource, TTarget>)map).Invoke(source, target);
            return;
        }

        var expression = GetRequiredRuntimeMap<TSource, TTarget>(null, _emptyParameters);

        if (expression.Body is not MemberInitExpression) {
            throw new NotSupportedException($"Mapping from TSource ({typeof(TSource).FullName}) to TTarget ({typeof(TTarget).FullName}) cannot map to an existing target instance because the map does not use an object initializer (x => new TTarget {{ ... }}). Use Map(source) instead.");
        }

        var compiled = CompileMapper(expression);
        _compiledMapToExistingCache[key] = compiled;
        compiled.Invoke(source, target);
    }

    /// <summary>
    /// Maps values from the source object to an existing target object using the default map and runtime parameters.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TTarget">The target type.</typeparam>
    /// <param name="source">The source object.</param>
    /// <param name="target">The target object to update.</param>
    /// <param name="parameters">Runtime parameters used by <see cref="MapifyProfile"/> <c>Parameter&lt;T&gt;(name)</c> markers.</param>
    /// <exception cref="ArgumentException">Thrown when no map is available.</exception>
    /// <exception cref="NotSupportedException">Thrown when the map cannot target an existing instance.</exception>
    public void Map<TSource, TTarget>(TSource source, TTarget target, IReadOnlyDictionary<string, object?> parameters) {
        ValidateRuntimeParameters(parameters);

        var expression = GetRequiredRuntimeMap<TSource, TTarget>(null, parameters);

        if (expression.Body is not MemberInitExpression) {
            throw new NotSupportedException($"Mapping from TSource ({typeof(TSource).FullName}) to TTarget ({typeof(TTarget).FullName}) cannot map to an existing target instance because the map does not use an object initializer (x => new TTarget {{ ... }}). Use Map(source, parameters) instead.");
        }

        var compiled = CompileMapper(expression);
        compiled.Invoke(source, target);
    }

    /// <summary>
    /// Maps the source object to a new target object using a named map.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TTarget">The target type.</typeparam>
    /// <param name="source">The source object.</param>
    /// <param name="name">The map name.</param>
    /// <returns>A new mapped target object.</returns>
    /// <exception cref="ArgumentException">Thrown when the name is invalid or the named map is missing.</exception>
    public TTarget Map<TSource, TTarget>(TSource source, string name) {
        if (string.IsNullOrWhiteSpace(name)) {
            throw new ArgumentException("Mapping name must not be null or whitespace.", nameof(name));
        }

        var key = new MapKey(typeof(TSource), typeof(TTarget), name);
        if (_compiledMapToNewCache.TryGetValue(key, out var map)) {
            return ((Func<TSource, TTarget>)map).Invoke(source);
        }

        var expression = GetRequiredRuntimeMap<TSource, TTarget>(name, _emptyParameters);
        var compiled = expression.Compile();
        _compiledMapToNewCache[key] = compiled;
        return compiled.Invoke(source);
    }

    /// <summary>
    /// Maps the source object to a new target object using a named map and runtime parameters.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TTarget">The target type.</typeparam>
    /// <param name="source">The source object.</param>
    /// <param name="name">The map name.</param>
    /// <param name="parameters">Runtime parameters used by <see cref="MapifyProfile"/> <c>Parameter&lt;T&gt;(name)</c> markers.</param>
    /// <returns>A new mapped target object.</returns>
    /// <exception cref="ArgumentException">Thrown when the name is invalid or the named map is missing.</exception>
    public TTarget Map<TSource, TTarget>(TSource source, string name, IReadOnlyDictionary<string, object?> parameters) {
        if (string.IsNullOrWhiteSpace(name)) {
            throw new ArgumentException("Mapping name must not be null or whitespace.", nameof(name));
        }

        ValidateRuntimeParameters(parameters);

        var expression = GetRequiredRuntimeMap<TSource, TTarget>(name, parameters);
        var compiled = expression.Compile();
        return compiled.Invoke(source);
    }

    /// <summary>
    /// Maps the source object to a new target object using the default map.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TTarget">The target type.</typeparam>
    /// <param name="source">The source object.</param>
    /// <returns>A new mapped target object.</returns>
    /// <exception cref="ArgumentException">Thrown when no map is available.</exception>
    public TTarget Map<TSource, TTarget>(TSource source) {
        var key = new MapKey(typeof(TSource), typeof(TTarget), null);
        if (_compiledMapToNewCache.TryGetValue(key, out var map)) {
            return ((Func<TSource, TTarget>)map).Invoke(source);
        }

        var expression = GetRequiredRuntimeMap<TSource, TTarget>(null, _emptyParameters);
        var compiled = expression.Compile();
        _compiledMapToNewCache[key] = compiled;
        return compiled.Invoke(source);
    }

    /// <summary>
    /// Maps the source object to a new target object using the default map and runtime parameters.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TTarget">The target type.</typeparam>
    /// <param name="source">The source object.</param>
    /// <param name="parameters">Runtime parameters used by <see cref="MapifyProfile"/> <c>Parameter&lt;T&gt;(name)</c> markers.</param>
    /// <returns>A new mapped target object.</returns>
    /// <exception cref="ArgumentException">Thrown when no map is available.</exception>
    public TTarget Map<TSource, TTarget>(TSource source, IReadOnlyDictionary<string, object?> parameters) {
        ValidateRuntimeParameters(parameters);

        var expression = GetRequiredRuntimeMap<TSource, TTarget>(null, parameters);
        var compiled = expression.Compile();
        return compiled.Invoke(source);
    }
}
