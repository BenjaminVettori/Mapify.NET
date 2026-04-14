using System.Linq.Expressions;

namespace Mapify.NET;

public partial class Mapify {
    private static bool TryBuildMappedExpression(
        Expression sourceAccess,
        Type sourceType,
        Type destinationType,
        Func<Type, Type, string?, LambdaExpression?> resolver,
        string? preferredMapName,
        out Expression mappedResult,
        out Expression? sourceNullCheck
    ) {
        mappedResult = null!;
        sourceNullCheck = null;

        if (TryBuildDirectMappedExpression(sourceAccess, sourceType, destinationType, resolver, preferredMapName, out mappedResult, out sourceNullCheck)) {
            return true;
        }

        if (TryBuildEnumerableMappedExpression(sourceAccess, sourceType, destinationType, resolver, preferredMapName, out mappedResult, out sourceNullCheck)) {
            return true;
        }

        return false;
    }

    private static bool TryBuildDirectMappedExpression(
        Expression sourceAccess,
        Type sourceType,
        Type destinationType,
        Func<Type, Type, string?, LambdaExpression?> resolver,
        string? preferredMapName,
        out Expression mappedResult,
        out Expression? sourceNullCheck
    ) {
        mappedResult = null!;
        sourceNullCheck = null;

        var mapExpr = ResolveMapByPrecedence(sourceType, destinationType, resolver, preferredMapName);
        if (mapExpr == null || mapExpr.Parameters.Count != 1 || mapExpr.ReturnType == typeof(void)) {
            return false;
        }

        if (!TryAdaptSourceForMap(sourceAccess, mapExpr.Parameters[0].Type, out var adaptedSource, out sourceNullCheck)) {
            return false;
        }

        var mappedBody = new ParameterReplaceVisitor(mapExpr.Parameters[0], adaptedSource).Visit(mapExpr.Body)!;

        if (TryAdaptMappedResult(mappedBody, destinationType, out mappedResult)) {
            return true;
        }

        if (TryGetEnumerableElementType(mappedBody.Type, out var mappedElementType)
            && TryGetEnumerableElementType(destinationType, out var destinationElementType)
            && mappedElementType == destinationElementType
            && TryMaterializeEnumerable(mappedBody, destinationType, destinationElementType, out mappedResult)) {
            return true;
        }

        return false;
    }

    private static bool TryBuildEnumerableMappedExpression(
        Expression sourceAccess,
        Type sourceType,
        Type destinationType,
        Func<Type, Type, string?, LambdaExpression?> resolver,
        string? preferredMapName,
        out Expression mappedResult,
        out Expression? sourceNullCheck
    ) {
        mappedResult = null!;
        sourceNullCheck = null;

        if (!TryGetEnumerableElementType(sourceType, out var sourceElementType)
            || !TryGetEnumerableElementType(destinationType, out var destinationElementType)
            || !TryGetEnumerableElementType(sourceAccess.Type, out var sourceAccessElementType)) {
            return false;
        }

        var elementMapExpr = ResolveMapByPrecedence(sourceElementType, destinationElementType, resolver, preferredMapName);
        var itemParam = Expression.Parameter(sourceAccessElementType, "e");
        Expression adaptedItemResult;
        Expression? itemNullCheck;

        if (elementMapExpr != null && elementMapExpr.Parameters.Count == 1 && elementMapExpr.ReturnType != typeof(void)) {
            if (!TryAdaptSourceForMap(itemParam, elementMapExpr.Parameters[0].Type, out var adaptedItem, out itemNullCheck)) {
                return false;
            }

            var mappedItemBody = new ParameterReplaceVisitor(elementMapExpr.Parameters[0], adaptedItem).Visit(elementMapExpr.Body)!;
            if (!TryAdaptMappedResult(mappedItemBody, destinationElementType, out adaptedItemResult)) {
                return false;
            }
        } else {
            if (!TryBuildMappedExpression(itemParam, sourceElementType, destinationElementType, resolver, preferredMapName, out adaptedItemResult, out itemNullCheck)) {
                if (!TryBuildImplicitEnumerableElementProjection(itemParam, destinationElementType, out adaptedItemResult, out itemNullCheck)) {
                    return false;
                }
            }
        }

        if (itemNullCheck != null) {
            adaptedItemResult = Expression.Condition(
                itemNullCheck,
                adaptedItemResult,
                CreateDefaultValueExpression(destinationElementType)
            );
        }

        var selector = Expression.Lambda(adaptedItemResult, itemParam);
        var selectExpr = Expression.Call(
            typeof(Enumerable),
            nameof(Enumerable.Select),
            [sourceAccessElementType, destinationElementType],
            sourceAccess,
            selector
        );

        if (!TryMaterializeEnumerable(selectExpr, destinationType, destinationElementType, out mappedResult)) {
            return false;
        }

        if (CanBeNull(sourceAccess.Type) && !IsInterfaceCollectionLikeType(sourceAccess.Type)) {
            sourceNullCheck = CreateHasValueCheck(sourceAccess);
        }

        return true;
    }

    private static bool TryBuildImplicitEnumerableElementProjection(
        ParameterExpression itemParam,
        Type destinationElementType,
        out Expression projection,
        out Expression? itemNullCheck
    ) {
        projection = null!;
        itemNullCheck = null;

        if (itemParam.Type == destinationElementType) {
            projection = itemParam;
            return true;
        }

        var sourceNullableType = Nullable.GetUnderlyingType(itemParam.Type);
        var destinationNullableType = Nullable.GetUnderlyingType(destinationElementType);

        if (destinationNullableType != null && itemParam.Type == destinationNullableType) {
            projection = Expression.Convert(itemParam, destinationElementType);
            return true;
        }

        if (destinationNullableType == null && sourceNullableType != null && destinationElementType == sourceNullableType) {
            itemNullCheck = Expression.NotEqual(itemParam, Expression.Constant(null, itemParam.Type));
            projection = Expression.Property(itemParam, "Value");
            return true;
        }

        if (destinationElementType.IsAssignableFrom(itemParam.Type)) {
            projection = itemParam;
            return true;
        }

        return false;
    }

    private static bool TryMaterializeEnumerable(
        Expression enumerableExpression,
        Type destinationType,
        Type destinationElementType,
        out Expression materialized
    ) {
        materialized = null!;

        if (destinationType.IsArray) {
            materialized = Expression.Call(
                typeof(Enumerable),
                nameof(Enumerable.ToArray),
                [destinationElementType],
                enumerableExpression
            );
            return true;
        }

        if (destinationType.IsAssignableFrom(enumerableExpression.Type)) {
            materialized = enumerableExpression;
            return true;
        }

        var toListExpr = Expression.Call(
            typeof(Enumerable),
            nameof(Enumerable.ToList),
            [destinationElementType],
            enumerableExpression
        );

        if (destinationType.IsAssignableFrom(toListExpr.Type)) {
            materialized = destinationType == toListExpr.Type
                ? toListExpr
                : Expression.Convert(toListExpr, destinationType);
            return true;
        }

        var ienumerableOfTarget = typeof(IEnumerable<>).MakeGenericType(destinationElementType);
        var ctor = destinationType.GetConstructor([ienumerableOfTarget]);
        if (ctor != null) {
            materialized = Expression.New(ctor, enumerableExpression);
            return true;
        }

        return false;
    }

    private static bool TryGetEnumerableElementType(Type type, out Type elementType) {
        elementType = null!;

        if (type == typeof(string)) {
            return false;
        }

        if (type.IsArray) {
            elementType = type.GetElementType()!;
            return true;
        }

        var enumerableElementTypes = type
            .GetInterfaces()
            .Concat([type])
            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            .Select(i => i.GetGenericArguments()[0])
            .Distinct()
            .ToArray();

        if (enumerableElementTypes.Length == 0) {
            return false;
        }

        if (enumerableElementTypes.Length > 1) {
            throw new InvalidOperationException($"Type '{type.FullName}' implements multiple IEnumerable<T> element types ({string.Join(", ", enumerableElementTypes.Select(x => x.FullName))}). Mapify cannot infer a single element type.");
        }

        elementType = enumerableElementTypes[0];
        return true;
    }

    private static bool TryCreateRuntimeMapExpression<TSource, TTarget>(
        Func<Type, Type, string?, LambdaExpression?> resolver,
        string? preferredMapName,
        out Expression<Func<TSource, TTarget>> expression
    ) {
        expression = null!;

        var sourceParam = Expression.Parameter(typeof(TSource), "x");
        if (!TryBuildMappedExpression(sourceParam, typeof(TSource), typeof(TTarget), resolver, preferredMapName, out var mappedBody, out _)) {
            return false;
        }

        expression = Expression.Lambda<Func<TSource, TTarget>>(mappedBody, sourceParam);
        return true;
    }

    private static LambdaExpression? ResolveMapByPrecedence(
        Type sourceType,
        Type destinationType,
        Func<Type, Type, string?, LambdaExpression?> resolver,
        string? preferredMapName
    ) {
        var sourceCoreType = Nullable.GetUnderlyingType(sourceType) ?? sourceType;
        var destinationCoreType = Nullable.GetUnderlyingType(destinationType) ?? destinationType;

        var resolved = resolver(sourceType, destinationType, preferredMapName)
            ?? resolver(sourceType, destinationCoreType, preferredMapName)
            ?? resolver(sourceCoreType, destinationType, preferredMapName)
            ?? resolver(sourceCoreType, destinationCoreType, preferredMapName);

        if (resolved != null) {
            return resolved;
        }

        return TryResolveAssignableCollectionMap(sourceType, destinationType, resolver, preferredMapName);
    }

    private static LambdaExpression? TryResolveAssignableCollectionMap(
        Type sourceType,
        Type destinationType,
        Func<Type, Type, string?, LambdaExpression?> resolver,
        string? preferredMapName
    ) {
        var sourceIsCollection = TryGetEnumerableElementType(sourceType, out _);
        var destinationIsCollection = TryGetEnumerableElementType(destinationType, out _);

        if (!sourceIsCollection && !destinationIsCollection) {
            return null;
        }

        var sourceCandidates = sourceIsCollection ? GetCollectionResolutionCandidates(sourceType) : [sourceType];
        var destinationCandidates = destinationIsCollection ? GetCollectionResolutionCandidates(destinationType) : [destinationType];

        foreach (var sourceCandidate in sourceCandidates) {
            foreach (var destinationCandidate in destinationCandidates) {
                if (sourceCandidate == sourceType && destinationCandidate == destinationType) {
                    continue;
                }

                var resolved = resolver(sourceCandidate, destinationCandidate, preferredMapName);
                if (resolved != null) {
                    return resolved;
                }
            }
        }

        return null;
    }

    private static IReadOnlyList<Type> GetCollectionResolutionCandidates(Type type) {
        var candidates = new List<Type> { type };

        foreach (var @interface in type.GetInterfaces()) {
            if (!@interface.IsGenericType) {
                continue;
            }

            var genericDef = @interface.GetGenericTypeDefinition();
            if (genericDef == typeof(IList<>)
                || genericDef == typeof(ICollection<>)
                || genericDef == typeof(IReadOnlyList<>)
                || genericDef == typeof(IReadOnlyCollection<>)
                || genericDef == typeof(IEnumerable<>)) {
                candidates.Add(@interface);
            }
        }

        return candidates
            .Distinct()
            .OrderBy(x => x == type ? 0 : 1)
            .ThenBy(GetCollectionCandidateRank)
            .ThenBy(x => x.FullName, StringComparer.Ordinal)
            .ToList();
    }

    private static int GetCollectionCandidateRank(Type type) {
        if (!type.IsGenericType) {
            return 50;
        }

        var genericDef = type.GetGenericTypeDefinition();
        if (genericDef == typeof(IList<>)) {
            return 1;
        }

        if (genericDef == typeof(ICollection<>)) {
            return 2;
        }

        if (genericDef == typeof(IReadOnlyList<>)) {
            return 3;
        }

        if (genericDef == typeof(IReadOnlyCollection<>)) {
            return 4;
        }

        if (genericDef == typeof(IEnumerable<>)) {
            return 5;
        }

        return 10;
    }

    private static bool TryAdaptSourceForMap(
        Expression sourceAccess,
        Type mapSourceType,
        out Expression adaptedSource,
        out Expression? sourceHasValueCheck
    ) {
        adaptedSource = sourceAccess;
        sourceHasValueCheck = null;
        var nestedSourceNullCheck = BuildNestedMemberAccessGuard(sourceAccess);

        if (sourceAccess.Type == mapSourceType) {
            if (CanBeNull(sourceAccess.Type) && !IsInterfaceCollectionLikeType(sourceAccess.Type)) {
                sourceHasValueCheck = CreateHasValueCheck(sourceAccess);
            }

            sourceHasValueCheck = CombineNullChecks(nestedSourceNullCheck, sourceHasValueCheck);
            return true;
        }

        var sourceNullableUnderlying = Nullable.GetUnderlyingType(sourceAccess.Type);
        var mapNullableUnderlying = Nullable.GetUnderlyingType(mapSourceType);

        if (sourceNullableUnderlying != null && sourceNullableUnderlying == mapSourceType) {
            sourceHasValueCheck = Expression.NotEqual(sourceAccess, Expression.Constant(null, sourceAccess.Type));
            sourceHasValueCheck = CombineNullChecks(nestedSourceNullCheck, sourceHasValueCheck);
            adaptedSource = Expression.Property(sourceAccess, "Value");
            return true;
        }

        if (mapNullableUnderlying != null && sourceAccess.Type == mapNullableUnderlying) {
            sourceHasValueCheck = CombineNullChecks(nestedSourceNullCheck, sourceHasValueCheck);
            adaptedSource = Expression.Convert(sourceAccess, mapSourceType);
            return true;
        }

        if (mapSourceType.IsAssignableFrom(sourceAccess.Type)) {
            if (CanBeNull(sourceAccess.Type) && !IsInterfaceCollectionLikeType(sourceAccess.Type)) {
                sourceHasValueCheck = CreateHasValueCheck(sourceAccess);
            }

            sourceHasValueCheck = CombineNullChecks(nestedSourceNullCheck, sourceHasValueCheck);
            adaptedSource = sourceAccess;
            return true;
        }

        return false;
    }

    private static Expression? CombineNullChecks(Expression? left, Expression? right) {
        if (left == null) {
            return right;
        }

        if (right == null) {
            return left;
        }

        return Expression.AndAlso(left, right);
    }

    private static bool TryAdaptMappedResult(Expression mappedBody, Type targetType, out Expression adaptedResult) {
        adaptedResult = mappedBody;

        if (mappedBody.Type == targetType) {
            return true;
        }

        var targetNullableUnderlying = Nullable.GetUnderlyingType(targetType);
        var mappedNullableUnderlying = Nullable.GetUnderlyingType(mappedBody.Type);

        if (targetNullableUnderlying != null && mappedBody.Type == targetNullableUnderlying) {
            adaptedResult = Expression.Convert(mappedBody, targetType);
            return true;
        }

        if (mappedNullableUnderlying != null && targetType == mappedNullableUnderlying) {
            var hasValue = Expression.NotEqual(mappedBody, Expression.Constant(null, mappedBody.Type));
            var value = Expression.Property(mappedBody, "Value");
            adaptedResult = Expression.Condition(hasValue, value, Expression.Default(targetType));
            return true;
        }

        if (targetType.IsAssignableFrom(mappedBody.Type)) {
            adaptedResult = mappedBody;
            return true;
        }

        return false;
    }

    private static bool CanBeNull(Type type)
        => !type.IsValueType || Nullable.GetUnderlyingType(type) != null;

    private static bool IsCollectionLikeType(Type type)
        => type != typeof(string)
           && typeof(System.Collections.IEnumerable).IsAssignableFrom(type)
           && type != typeof(byte[]);

    private static bool IsInterfaceCollectionLikeType(Type type)
        => type.IsInterface && IsCollectionLikeType(type);

    private static Expression CreateHasValueCheck(Expression valueExpression) {
        var checkExpression = IsInterfaceCollectionLikeType(valueExpression.Type)
            ? Expression.Convert(valueExpression, typeof(object))
            : valueExpression;

        return Expression.NotEqual(checkExpression, Expression.Constant(null, checkExpression.Type));
    }
}
