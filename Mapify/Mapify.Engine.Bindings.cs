using System.Linq.Expressions;
using System.Reflection;

namespace Mapify.NET;

public partial class Mapify {
    private static Expression<Func<TSource, TDestination>> CreateMap<TSource, TDestination>(
        Expression<Func<TSource, TDestination>>? partial,
        IReadOnlyList<MapBuilderBinding>? mapBuilderBindings,
        Func<Type, Type, string?, LambdaExpression?>? existingMapResolver
    ) {
        var baseParam = Expression.Parameter(typeof(TSource), "x");

        var sourceProperties = typeof(TSource).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead)
            .ToDictionary(p => p.Name);

        var existingBindings = new Dictionary<string, MemberBinding>();
        var ignoredBindings = new HashSet<string>(StringComparer.Ordinal);
        if (partial != null) {
            var partialUpdated = (MemberInitExpression)new ParameterReplaceVisitor(partial.Parameters[0], baseParam)
                .Visit(partial.Body)!;

            foreach (var partialBinding in partialUpdated.Bindings.OfType<MemberAssignment>()) {
                var binding = MapPartialBinding(partialBinding, typeof(TDestination), existingMapResolver, out var isIgnored);
                if (isIgnored) {
                    ignoredBindings.Add(partialBinding.Member.Name);
                }

                if (binding != null) {
                    existingBindings[binding.Member.Name] = binding;
                }
            }
        }

        if (mapBuilderBindings != null) {
            foreach (var builderBinding in mapBuilderBindings) {
                var binding = MapMapBuilderBinding<TSource, TDestination>(builderBinding, baseParam, existingMapResolver, out var destinationProperty, out var isIgnored);
                if (isIgnored) {
                    ignoredBindings.Add(destinationProperty.Name);
                }

                if (binding == null) {
                    continue;
                }

                existingBindings[destinationProperty.Name] = binding;
                if (!isIgnored) {
                    ignoredBindings.Remove(destinationProperty.Name);
                }
            }
        }

        var destinationProperties = typeof(TDestination).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite);

        var allBindings = new List<MemberBinding>(existingBindings.Values);

        foreach (var destProp in destinationProperties) {
            if (existingBindings.ContainsKey(destProp.Name) || ignoredBindings.Contains(destProp.Name)) {
                continue;
            }

            var sourceProp = GetSourceProperty(sourceProperties, destProp);

            if (sourceProp != null) {
                if (TryGetBindingFromExistingMap(baseParam, sourceProp, destProp, existingMapResolver, out var mappedBinding)) {
                    allBindings.Add(mappedBinding);
                } else if (TryGetImplicitBinding(baseParam, sourceProp, destProp, out var implicitBinding)) {
                    allBindings.Add(implicitBinding);
                }
            } else if (TryGetDefaultCollectionBindingForUnmappedDestination(destProp, typeof(TDestination), out var defaultCollectionBinding)) {
                allBindings.Add(defaultCollectionBinding);
            }
        }

        var body = Expression.MemberInit(Expression.New(typeof(TDestination)), allBindings);
        var result = Expression.Lambda<Func<TSource, TDestination>>(body, baseParam);

        if (ignoredBindings.Count > 0) {
            _ignoredMembersByMap.Remove(result);
            _ignoredMembersByMap.Add(result, ignoredBindings);
        }

        return result;
    }

    private static MemberAssignment? MapMapBuilderBinding<TSource, TDestination>(
        MapBuilderBinding builderBinding,
        ParameterExpression sourceParameter,
        Func<Type, Type, string?, LambdaExpression?>? existingMapResolver,
        out PropertyInfo destinationProperty,
        out bool isIgnored
    ) {
        destinationProperty = GetMapBuilderDestinationProperty<TSource, TDestination>(builderBinding.TargetExpression);

        if (builderBinding.SourceExpression.Parameters.Count != 1 || builderBinding.SourceExpression.Parameters[0].Type != typeof(TSource)) {
            throw new InvalidOperationException($"DSL map binding for '{typeof(TSource).FullName} -> {typeof(TDestination).FullName}' requires a source selector with exactly one parameter of type '{typeof(TSource).FullName}'.");
        }

        var sourceExpression = new ParameterReplaceVisitor(builderBinding.SourceExpression.Parameters[0], sourceParameter)
            .Visit(builderBinding.SourceExpression.Body)!;
        return MapPartialBinding(destinationProperty, sourceExpression, typeof(TDestination), existingMapResolver, out isIgnored, tryMapExpressionToDestinationProperty: true, dslSourceType: typeof(TSource));
    }

    private static PropertyInfo GetMapBuilderDestinationProperty<TSource, TDestination>(LambdaExpression destinationExpression) {
        if (destinationExpression.Parameters.Count != 1 || destinationExpression.Parameters[0].Type != typeof(TDestination)) {
            throw new InvalidOperationException($"DSL map binding for '{typeof(TSource).FullName} -> {typeof(TDestination).FullName}' requires a destination selector with exactly one parameter of type '{typeof(TDestination).FullName}'.");
        }

        var destinationBody = UnwrapConvert(destinationExpression.Body);
        if (destinationBody is not MemberExpression memberExpression
            || memberExpression.Expression != destinationExpression.Parameters[0]
            || memberExpression.Member is not PropertyInfo propertyInfo) {
            throw new InvalidOperationException($"DSL map binding for '{typeof(TSource).FullName} -> {typeof(TDestination).FullName}' requires destination selector to be a direct writable property access (for example: d => d.Property).");
        }

        if (!propertyInfo.CanWrite) {
            throw new InvalidOperationException($"DSL map binding for '{typeof(TSource).FullName} -> {typeof(TDestination).FullName}' cannot target non-writable property '{propertyInfo.Name}'.");
        }

        return propertyInfo;
    }

    private static MemberAssignment? MapPartialBinding(
        MemberAssignment partialBinding,
        Type destinationType,
        Func<Type, Type, string?, LambdaExpression?>? existingMapResolver,
        out bool isIgnored,
        bool tryMapExpressionToDestinationProperty = false,
        Type? dslSourceType = null
    ) {
        return MapPartialBinding(partialBinding.Member, partialBinding.Expression, destinationType, existingMapResolver, out isIgnored, tryMapExpressionToDestinationProperty, dslSourceType);
    }

    private static MemberAssignment? MapPartialBinding(
        MemberInfo destinationMember,
        Expression destinationExpression,
        Type destinationType,
        Func<Type, Type, string?, LambdaExpression?>? existingMapResolver,
        out bool isIgnored,
        bool tryMapExpressionToDestinationProperty = false,
        Type? dslSourceType = null
    ) {
        isIgnored = false;

        var expr = destinationExpression;
        var destinationProperty = destinationMember as PropertyInfo;

        if (TryResolveIgnoreMarker(destinationMember, expr, destinationType, out var ignoredBinding)) {
            isIgnored = true;
            return ignoredBinding;
        }

        if (TryResolveUseMapMarker(destinationMember, expr, existingMapResolver, out var mappedBinding)) {
            return mappedBinding;
        }

        if (existingMapResolver != null) {
            expr = new UseMapMarkerReplaceVisitor(existingMapResolver).Visit(expr)!;
        }

        if (expr is BinaryExpression binaryExpr && binaryExpr.NodeType == ExpressionType.Coalesce) {
            expr = NormalizeCoalesceToConditional(binaryExpr);
        }

        if (tryMapExpressionToDestinationProperty && destinationProperty != null) {
            if (!TryAdaptMappedResult(expr, destinationProperty.PropertyType, out var adaptedExpression)) {
                if (existingMapResolver == null
                    || !TryBuildMappedExpression(expr, expr.Type, destinationProperty.PropertyType, existingMapResolver, null, out adaptedExpression, out var sourceNullCheck)) {
                    var mappingTypes = dslSourceType == null
                        ? destinationType.FullName
                        : $"{dslSourceType.FullName} -> {destinationType.FullName}";
                    throw new InvalidOperationException($"DSL map binding for '{mappingTypes}' is missing type map configuration from '{expr.Type.FullName}' to '{destinationProperty.PropertyType.FullName}' for destination property '{destinationProperty.Name}'.");
                }

                if (sourceNullCheck != null) {
                    var nullFallback = CreatePropertyDefaultValueExpression(destinationProperty);
                    if (nullFallback.Type != adaptedExpression.Type && adaptedExpression.Type.IsAssignableFrom(nullFallback.Type)) {
                        nullFallback = Expression.Convert(nullFallback, adaptedExpression.Type);
                    }

                    adaptedExpression = Expression.Condition(sourceNullCheck, adaptedExpression, nullFallback);
                }
            }

            expr = adaptedExpression;
        }

        expr = ApplyNestedNullSafety(expr, destinationProperty);

        return Expression.Bind(destinationMember, expr);
    }


    private static PropertyInfo? GetSourceProperty(Dictionary<string, PropertyInfo> sourceTypeProperties, PropertyInfo destProp) {
        if (!sourceTypeProperties.TryGetValue(destProp.Name, out var sourceProp)) {
            return null;
        }

        return sourceProp;
    }

    private static bool TryGetImplicitBinding(
        ParameterExpression baseParam,
        PropertyInfo sourceProp,
        PropertyInfo destProp,
        out MemberAssignment binding
    ) {
        binding = null!;

        var sourceNullableType = Nullable.GetUnderlyingType(sourceProp.PropertyType);

        var isCompatibleType =
            destProp.PropertyType.IsAssignableFrom(sourceProp.PropertyType)
            || (sourceNullableType != null && destProp.PropertyType.IsAssignableFrom(sourceNullableType));

        if (!isCompatibleType) {
            return false;
        }

        binding = GetImplicitBinding(baseParam, sourceProp, destProp);
        return true;
    }

    private static MemberAssignment GetImplicitBinding(ParameterExpression baseParam, PropertyInfo sourceProp, PropertyInfo destProp) {
        MemberAssignment binding;
        var sourceAccess = Expression.Property(baseParam, sourceProp);

        var sourceNullableType = Nullable.GetUnderlyingType(sourceProp.PropertyType);
        var destNullableType = Nullable.GetUnderlyingType(destProp.PropertyType);

        if (destNullableType != null && sourceProp.PropertyType == destNullableType) {
            var nullableType = typeof(Nullable<>).MakeGenericType(destNullableType);
            var converted = Expression.Convert(sourceAccess, nullableType);
            binding = Expression.Bind(destProp, converted);
        } else if (destNullableType == null && sourceNullableType != null && destProp.PropertyType == sourceNullableType) {
            var notNull = Expression.NotEqual(sourceAccess, Expression.Constant(null, sourceAccess.Type));
            var value = Expression.Property(sourceAccess, "Value");
            var defaultValue = Expression.Default(destProp.PropertyType);
            var conditional = Expression.Condition(notNull, value, defaultValue);
            binding = Expression.Bind(destProp, conditional);
        } else {
            binding = Expression.Bind(destProp, sourceAccess);
        }

        return binding;
    }

    private static bool TryGetBindingFromExistingMap(
        ParameterExpression baseParam,
        PropertyInfo sourceProp,
        PropertyInfo destProp,
        Func<Type, Type, string?, LambdaExpression?>? existingMapResolver,
        out MemberAssignment binding
    ) {
        binding = null!;

        if (existingMapResolver == null) {
            return false;
        }

        var sourceAccess = Expression.Property(baseParam, sourceProp);

        if (!TryBuildMappedExpression(sourceAccess, sourceProp.PropertyType, destProp.PropertyType, existingMapResolver, null, out var adaptedResult, out var sourceNullCheck)) {
            return false;
        }

        if (sourceNullCheck != null) {
            var nullFallback = CreatePropertyDefaultValueExpression(destProp);
            if (nullFallback.Type != adaptedResult.Type && adaptedResult.Type.IsAssignableFrom(nullFallback.Type)) {
                nullFallback = Expression.Convert(nullFallback, adaptedResult.Type);
            }

            adaptedResult = Expression.Condition(sourceNullCheck, adaptedResult, nullFallback);
        }

        binding = Expression.Bind(destProp, adaptedResult);
        return true;
    }

    private static bool TryGetDefaultCollectionBindingForUnmappedDestination(
        PropertyInfo destinationProperty,
        Type destinationType,
        out MemberAssignment binding
    ) {
        binding = null!;

        if (!IsCollectionLikeType(destinationProperty.PropertyType)) {
            return false;
        }

        if (IsPropertyInitializedOnFreshInstance(destinationProperty, destinationType)) {
            return false;
        }

        binding = Expression.Bind(destinationProperty, CreatePropertyDefaultValueExpression(destinationProperty));
        return true;
    }

}
