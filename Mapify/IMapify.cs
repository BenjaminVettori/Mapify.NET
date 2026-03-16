using System.Linq.Expressions;

namespace Mapify.NET; 
/// <summary>
/// Contract for an instance-based mapper that stores map registrations and compiled delegates per mapper instance.
/// </summary>
public interface IMapify {
    /// <summary>
    /// Configures whether a default map should be generated when an explicit type map is missing.
    /// </summary>
    /// <param name="value">True to enable default-map fallback; otherwise false.</param>
    void UseDefaultMapIfTypeMapIsMissing(bool value);

    /// <summary>
    /// Gets the default map expression for the source and target types.
    /// Returns <c>null</c> when no map exists and default-map fallback is disabled.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TTarget">The target type.</typeparam>
    /// <returns>The mapping expression, or <c>null</c> if not found and fallback is disabled.</returns>
    public Expression<Func<TSource, TTarget>>? GetMap<TSource, TTarget>();

    /// <summary>
    /// Gets the default map expression for the source and target types and replaces runtime parameters.
    /// Returns <c>null</c> when no map exists and default-map fallback is disabled.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TTarget">The target type.</typeparam>
    /// <param name="parameters">Runtime parameters used by <see cref="MapifyProfile"/> <c>Parameter&lt;T&gt;(name)</c> markers.</param>
    /// <returns>The mapping expression, or <c>null</c> if not found and fallback is disabled.</returns>
    public Expression<Func<TSource, TTarget>>? GetMap<TSource, TTarget>(IReadOnlyDictionary<string, object?> parameters);

    /// <summary>
    /// Gets the default map expression for the source and target types, throwing if none is available.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TTarget">The target type.</typeparam>
    /// <returns>The required mapping expression.</returns>
    /// <exception cref="ArgumentException">Thrown when no map is available.</exception>
    public Expression<Func<TSource, TTarget>> GetRequiredMap<TSource, TTarget>();

    /// <summary>
    /// Gets the default map expression for the source and target types, replaces runtime parameters, and throws if none is available.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TTarget">The target type.</typeparam>
    /// <param name="parameters">Runtime parameters used by <see cref="MapifyProfile"/> <c>Parameter&lt;T&gt;(name)</c> markers.</param>
    /// <returns>The required mapping expression.</returns>
    /// <exception cref="ArgumentException">Thrown when no map is available.</exception>
    public Expression<Func<TSource, TTarget>> GetRequiredMap<TSource, TTarget>(IReadOnlyDictionary<string, object?> parameters);

    /// <summary>
    /// Gets the named map expression for the source and target types.
    /// Returns <c>null</c> when the named map is missing.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TTarget">The target type.</typeparam>
    /// <param name="name">The map name.</param>
    /// <returns>The mapping expression, or <c>null</c> if not found.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is null, empty, or whitespace.</exception>
    public Expression<Func<TSource, TTarget>>? GetMap<TSource, TTarget>(string name);

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
    public Expression<Func<TSource, TTarget>>? GetMap<TSource, TTarget>(string name, IReadOnlyDictionary<string, object?> parameters);

    /// <summary>
    /// Gets the named map expression for the source and target types, throwing if it is missing.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TTarget">The target type.</typeparam>
    /// <param name="name">The map name.</param>
    /// <returns>The required named mapping expression.</returns>
    /// <exception cref="ArgumentException">Thrown when the name is invalid or the named map is missing.</exception>
    public Expression<Func<TSource, TTarget>> GetRequiredMap<TSource, TTarget>(string name);

    /// <summary>
    /// Gets the named map expression for the source and target types, replaces runtime parameters, and throws if it is missing.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TTarget">The target type.</typeparam>
    /// <param name="name">The map name.</param>
    /// <param name="parameters">Runtime parameters used by <see cref="MapifyProfile"/> <c>Parameter&lt;T&gt;(name)</c> markers.</param>
    /// <returns>The required named mapping expression.</returns>
    /// <exception cref="ArgumentException">Thrown when the name is invalid or the named map is missing.</exception>
    public Expression<Func<TSource, TTarget>> GetRequiredMap<TSource, TTarget>(string name, IReadOnlyDictionary<string, object?> parameters);

    /// <summary>
    /// Maps values from the source object to an existing target object using the default map.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TTarget">The target type.</typeparam>
    /// <param name="source">The source object.</param>
    /// <param name="target">The target object to update.</param>
    void Map<TSource, TTarget>(TSource source, TTarget target);

    /// <summary>
    /// Maps values from the source object to an existing target object using a named map.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TTarget">The target type.</typeparam>
    /// <param name="source">The source object.</param>
    /// <param name="target">The target object to update.</param>
    /// <param name="name">The map name.</param>
    void Map<TSource, TTarget>(TSource source, TTarget target, string name);

    /// <summary>
    /// Maps the source object to a new target object using the default map.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TTarget">The target type.</typeparam>
    /// <param name="source">The source object.</param>
    /// <returns>A new mapped target object.</returns>
    TTarget Map<TSource, TTarget>(TSource source);

    /// <summary>
    /// Maps the source object to a new target object using a named map.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TTarget">The target type.</typeparam>
    /// <param name="source">The source object.</param>
    /// <param name="name">The map name.</param>
    /// <returns>A new mapped target object.</returns>
    TTarget Map<TSource, TTarget>(TSource source, string name);

    /// <summary>
    /// Maps the source object to a new target object using the default map and runtime parameters.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TTarget">The target type.</typeparam>
    /// <param name="source">The source object.</param>
    /// <param name="parameters">Runtime parameters used by <see cref="MapifyProfile"/> <c>Parameter&lt;T&gt;(name)</c> markers.</param>
    /// <returns>A new mapped target object.</returns>
    TTarget Map<TSource, TTarget>(TSource source, IReadOnlyDictionary<string, object?> parameters);

    /// <summary>
    /// Maps the source object to a new target object using a named map and runtime parameters.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TTarget">The target type.</typeparam>
    /// <param name="source">The source object.</param>
    /// <param name="name">The map name.</param>
    /// <param name="parameters">Runtime parameters used by <see cref="MapifyProfile"/> <c>Parameter&lt;T&gt;(name)</c> markers.</param>
    /// <returns>A new mapped target object.</returns>
    TTarget Map<TSource, TTarget>(TSource source, string name, IReadOnlyDictionary<string, object?> parameters);

    /// <summary>
    /// Maps values from the source object to an existing target object using the default map and runtime parameters.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TTarget">The target type.</typeparam>
    /// <param name="source">The source object.</param>
    /// <param name="target">The target object to update.</param>
    /// <param name="parameters">Runtime parameters used by <see cref="MapifyProfile"/> <c>Parameter&lt;T&gt;(name)</c> markers.</param>
    void Map<TSource, TTarget>(TSource source, TTarget target, IReadOnlyDictionary<string, object?> parameters);

    /// <summary>
    /// Maps values from the source object to an existing target object using a named map and runtime parameters.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TTarget">The target type.</typeparam>
    /// <param name="source">The source object.</param>
    /// <param name="target">The target object to update.</param>
    /// <param name="name">The map name.</param>
    /// <param name="parameters">Runtime parameters used by <see cref="MapifyProfile"/> <c>Parameter&lt;T&gt;(name)</c> markers.</param>
    void Map<TSource, TTarget>(TSource source, TTarget target, string name, IReadOnlyDictionary<string, object?> parameters);
}
