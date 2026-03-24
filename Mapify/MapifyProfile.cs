using System.Linq.Expressions;

namespace Mapify.NET; 
internal interface IMapifyConfigurator {
    MapifyMapBuilder<TSource, TTarget> CreateMap<TSource, TTarget>(Expression<Func<TSource, TTarget>>? partial = null);

    MapifyMapBuilder<TSource, TTarget> CreateMap<TSource, TTarget>(string name, Expression<Func<TSource, TTarget>>? partial = null);
}

/// <summary>
/// Base class for defining mapping profiles.
/// </summary>
public abstract class MapifyProfile {
    private IMapifyConfigurator? _configurator;

    internal void Apply(IMapifyConfigurator configurator) {
        _configurator = configurator;
        try {
            Configure();
        } finally {
            _configurator = null;
        }
    }

    /// <summary>
    /// Defines mapping registrations for this profile.
    /// </summary>
    /// <remarks>
    /// Call <see cref="CreateMap{TSource, TTarget}(Expression{Func{TSource, TTarget}})"/>
    /// or <see cref="CreateMap{TSource, TTarget}(string, Expression{Func{TSource, TTarget}})"/>
    /// inside this method to register mappings.
    /// </remarks>
    protected abstract void Configure();

    /// <summary>
    /// Registers a default mapping from <typeparamref name="TSource"/> to <typeparamref name="TTarget"/>.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TTarget">The target type.</typeparam>
    /// <param name="partial">
    /// Optional partial initializer expression used to override selected destination bindings.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when called outside profile configuration.
    /// </exception>
    protected MapifyMapBuilder<TSource, TTarget> CreateMap<TSource, TTarget>(Expression<Func<TSource, TTarget>>? partial = null) {
        if (_configurator == null) {
            throw new InvalidOperationException("CreateMap can only be called while configuring a profile.");
        }

        return _configurator.CreateMap(partial);
    }

    /// <summary>
    /// Registers a named mapping from <typeparamref name="TSource"/> to <typeparamref name="TTarget"/>.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TTarget">The target type.</typeparam>
    /// <param name="name">The mapping name.</param>
    /// <param name="partial">
    /// Optional partial initializer expression used to override selected destination bindings.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when called outside profile configuration.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="name"/> is null, empty, or whitespace.
    /// </exception>
    protected MapifyMapBuilder<TSource, TTarget> CreateMap<TSource, TTarget>(string name, Expression<Func<TSource, TTarget>>? partial = null) {
        if (_configurator == null) {
            throw new InvalidOperationException("CreateMap can only be called while configuring a profile.");
        }

        if (string.IsNullOrWhiteSpace(name)) {
            throw new ArgumentException("Mapping name must not be null or whitespace.", nameof(name));
        }

        return _configurator.CreateMap(name, partial);
    }

    /// <summary>
    /// Marker used inside mapping expressions to force using a registered map
    /// from <typeparamref name="TSource"/> to <typeparamref name="TTarget"/>.
    /// </summary>
    /// <typeparam name="TSource">The source member type.</typeparam>
    /// <typeparam name="TTarget">The destination member type.</typeparam>
    /// <param name="source">The source expression to map.</param>
    /// <returns>Never returns; this method is only a marker in expression trees.</returns>
    protected static TTarget UseMap<TSource, TTarget>(TSource source) {
        throw new InvalidOperationException($"{nameof(UseMap)} can only be used as a marker inside a mapping expression during profile configuration.");
    }

    /// <summary>
    /// Marker used inside mapping expressions to force using a registered map
    /// from <typeparamref name="TSource"/> to <typeparamref name="TTarget"/>
    /// with an explicit recursion depth.
    /// </summary>
    /// <typeparam name="TSource">The source member type.</typeparam>
    /// <typeparam name="TTarget">The destination member type.</typeparam>
    /// <param name="source">The source expression to map.</param>
    /// <param name="maxDepth">The maximum recursion depth for this marker.</param>
    /// <returns>Never returns; this method is only a marker in expression trees.</returns>
    protected static TTarget UseMap<TSource, TTarget>(TSource source, int maxDepth) {
        throw new InvalidOperationException($"{nameof(UseMap)} can only be used as a marker inside a mapping expression during profile configuration.");
    }

    /// <summary>
    /// Marker used inside mapping expressions to force using a specific named map
    /// from <typeparamref name="TSource"/> to <typeparamref name="TTarget"/>.
    /// </summary>
    /// <typeparam name="TSource">The source member type.</typeparam>
    /// <typeparam name="TTarget">The destination member type.</typeparam>
    /// <param name="name">The mapping name to resolve.</param>
    /// <param name="source">The source expression to map.</param>
    /// <returns>Never returns; this method is only a marker in expression trees.</returns>
    protected static TTarget UseMap<TSource, TTarget>(string name, TSource source) {
        throw new InvalidOperationException($"{nameof(UseMap)} can only be used as a marker inside a mapping expression during profile configuration.");
    }

    /// <summary>
    /// Marker used inside mapping expressions to force using a specific named map
    /// from <typeparamref name="TSource"/> to <typeparamref name="TTarget"/>
    /// with an explicit recursion depth.
    /// </summary>
    /// <typeparam name="TSource">The source member type.</typeparam>
    /// <typeparam name="TTarget">The destination member type.</typeparam>
    /// <param name="name">The mapping name to resolve.</param>
    /// <param name="source">The source expression to map.</param>
    /// <param name="maxDepth">The maximum recursion depth for this marker.</param>
    /// <returns>Never returns; this method is only a marker in expression trees.</returns>
    protected static TTarget UseMap<TSource, TTarget>(string name, TSource source, int maxDepth) {
        throw new InvalidOperationException($"{nameof(UseMap)} can only be used as a marker inside a mapping expression during profile configuration.");
    }

    /// <summary>
    /// Marker used inside <see cref="CreateMap{TSource, TTarget}(Expression{Func{TSource, TTarget}})"/> expressions
    /// to ignore a destination property binding.
    /// </summary>
    /// <typeparam name="T">The destination property type.</typeparam>
    /// <returns>Never returns; this method is only a marker in expression trees.</returns>
    protected static T Ignore<T>() {
        throw new InvalidOperationException($"{nameof(Ignore)} can only be used as a marker inside a mapping expression during profile configuration.");
    }

    /// <summary>
    /// Marker used inside mapping expressions to inject a runtime parameter value.
    /// </summary>
    /// <typeparam name="T">The expected parameter type.</typeparam>
    /// <param name="parameterName">The parameter name.</param>
    /// <returns>Never returns; this method is only a marker in expression trees.</returns>
    protected static T Parameter<T>(string parameterName) {
        throw new InvalidOperationException($"{nameof(Parameter)} can only be used as a marker inside a mapping expression during profile configuration.");
    }
}
