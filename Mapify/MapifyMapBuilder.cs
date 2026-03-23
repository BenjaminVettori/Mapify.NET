using System.Linq.Expressions;

namespace Mapify.NET;

/// <summary>
/// Fluent builder for adding per-property bindings to a registered map.
/// </summary>
/// <typeparam name="TSource">Source type.</typeparam>
/// <typeparam name="TTarget">Target type.</typeparam>
public sealed class MapifyMapBuilder<TSource, TTarget> {
    private readonly Action<LambdaExpression, LambdaExpression> _addBinding;

    internal MapifyMapBuilder(Action<LambdaExpression, LambdaExpression> addBinding) {
        _addBinding = addBinding;
    }

    /// <summary>
    /// Adds or overrides a binding for a single destination property.
    /// </summary>
    /// <typeparam name="TTargetMember">Destination property type.</typeparam>
    /// <typeparam name="TSourceMember">Source expression type.</typeparam>
    /// <param name="target">Destination property selector.</param>
    /// <param name="source">Source value expression.</param>
    /// <returns>The current map builder.</returns>
    public MapifyMapBuilder<TSource, TTarget> Map<TTargetMember, TSourceMember>(
        Expression<Func<TTarget, TTargetMember>> target,
        Expression<Func<TSource, TSourceMember>> source
    ) {
        if (target == null) {
            throw new ArgumentNullException(nameof(target));
        }

        if (source == null) {
            throw new ArgumentNullException(nameof(source));
        }

        _addBinding(target, source);
        return this;
    }
}
