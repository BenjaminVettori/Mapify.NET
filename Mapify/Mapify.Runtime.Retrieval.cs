using System.Linq.Expressions;

namespace Mapify.NET;

public partial class Mapify {
    /// <summary>
    /// Gets the named map expression for the source and target types.
    /// Returns <c>null</c> when the named map is missing.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TTarget">The target type.</typeparam>
    /// <param name="name">The map name.</param>
    /// <returns>The mapping expression, or <c>null</c> if not found.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is null, empty, or whitespace.</exception>
    public Expression<Func<TSource, TTarget>>? GetMap<TSource, TTarget>(string name) {
        return GetMap<TSource, TTarget>(name, _emptyParameters);
    }

    /// <summary>
    /// Gets the named map expression for the source and target types and replaces runtime parameters.
    /// Returns <c>null</c> when the named map is missing.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TTarget">The target type.</typeparam>
    /// <param name="name">The map name.</param>
    /// <param name="parameters">Runtime parameters used by <see cref="MapifyProfile"/> <c>Parameter&lt;T&gt;(name)</c> markers.</param>
    /// <returns>The mapping expression, or <c>null</c> if not found.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is null, empty, or whitespace.</exception>
    public Expression<Func<TSource, TTarget>>? GetMap<TSource, TTarget>(string name, IReadOnlyDictionary<string, object?> parameters) {
        if (string.IsNullOrWhiteSpace(name)) {
            throw new ArgumentException("Mapping name must not be null or whitespace.", nameof(name));
        }

        ValidateRuntimeParameters(parameters);

        var key = new MapKey(typeof(TSource), typeof(TTarget), name);
        if (_converters.TryGetValue(key, out var existingConverter)) {
            return ApplyParameters((Expression<Func<TSource, TTarget>>)existingConverter, parameters);
        }

        return null;
    }

    /// <summary>
    /// Gets the named map expression for the source and target types, throwing if it is missing.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TTarget">The target type.</typeparam>
    /// <param name="name">The map name.</param>
    /// <returns>The required named mapping expression.</returns>
    /// <exception cref="ArgumentException">Thrown when the name is invalid or the named map is missing.</exception>
    public Expression<Func<TSource, TTarget>> GetRequiredMap<TSource, TTarget>(string name) {
        return GetRequiredMap<TSource, TTarget>(name, _emptyParameters);
    }

    /// <summary>
    /// Gets the named map expression for the source and target types, replaces runtime parameters, and throws if it is missing.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TTarget">The target type.</typeparam>
    /// <param name="name">The map name.</param>
    /// <param name="parameters">Runtime parameters used by <see cref="MapifyProfile"/> <c>Parameter&lt;T&gt;(name)</c> markers.</param>
    /// <returns>The required named mapping expression.</returns>
    /// <exception cref="ArgumentException">Thrown when the name is invalid or the named map is missing.</exception>
    public Expression<Func<TSource, TTarget>> GetRequiredMap<TSource, TTarget>(string name, IReadOnlyDictionary<string, object?> parameters) {
        var map = GetMap<TSource, TTarget>(name, parameters);
        if (map != null) {
            return map;
        }

        throw new ArgumentException($"Missing named type map configuration '{name}' for TSource ({typeof(TSource).FullName}) to TTarget ({typeof(TTarget).FullName})");
    }

    /// <summary>
    /// Gets the default map expression for the source and target types.
    /// Returns <c>null</c> when no map exists and default-map fallback is disabled.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TTarget">The target type.</typeparam>
    /// <returns>The mapping expression, or <c>null</c> if not found and fallback is disabled.</returns>
    public Expression<Func<TSource, TTarget>>? GetMap<TSource, TTarget>() {
        return GetMap<TSource, TTarget>(_emptyParameters);
    }

    /// <summary>
    /// Gets the default map expression for the source and target types and replaces runtime parameters.
    /// Returns <c>null</c> when no map exists and default-map fallback is disabled.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TTarget">The target type.</typeparam>
    /// <param name="parameters">Runtime parameters used by <see cref="MapifyProfile"/> <c>Parameter&lt;T&gt;(name)</c> markers.</param>
    /// <returns>The mapping expression, or <c>null</c> if not found and fallback is disabled.</returns>
    public Expression<Func<TSource, TTarget>>? GetMap<TSource, TTarget>(IReadOnlyDictionary<string, object?> parameters) {
        ValidateRuntimeParameters(parameters);

        var key = new MapKey(typeof(TSource), typeof(TTarget), null);
        if (_converters.TryGetValue(key, out var existingConverter)) {
            return ApplyParameters((Expression<Func<TSource, TTarget>>)existingConverter, parameters);
        }

        if (_useDefaultMapIfTypeMapIsMissing) {
            return GetOrCreateDefaultMap<TSource, TTarget>(parameters);
        }

        return null;
    }

    /// <summary>
    /// Gets the default map expression for the source and target types, throwing if none is available.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TTarget">The target type.</typeparam>
    /// <returns>The required mapping expression.</returns>
    /// <exception cref="ArgumentException">Thrown when no map is available.</exception>
    public Expression<Func<TSource, TTarget>> GetRequiredMap<TSource, TTarget>() {
        return GetRequiredMap<TSource, TTarget>(_emptyParameters);
    }

    /// <summary>
    /// Gets the default map expression for the source and target types, replaces runtime parameters, and throws if none is available.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TTarget">The target type.</typeparam>
    /// <param name="parameters">Runtime parameters used by <see cref="MapifyProfile"/> <c>Parameter&lt;T&gt;(name)</c> markers.</param>
    /// <returns>The required mapping expression.</returns>
    /// <exception cref="ArgumentException">Thrown when no map is available.</exception>
    public Expression<Func<TSource, TTarget>> GetRequiredMap<TSource, TTarget>(IReadOnlyDictionary<string, object?> parameters) {
        var map = GetMap<TSource, TTarget>(parameters);
        if (map != null) {
            return map;
        }

        throw new ArgumentException($"Missing type map configuration for TSource ({typeof(TSource).FullName}) to TTarget ({typeof(TTarget).FullName})");
    }
}
