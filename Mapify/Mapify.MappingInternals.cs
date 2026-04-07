using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Mapify.NET;

public partial class Mapify {
    private const string _useMapMarkerName = "UseMap";

    private const string _ignoreMarkerName = "Ignore";

    private const string _projectToMarkerName = "ProjectTo";

    private const string _parameterMarkerName = "Parameter";

    private static readonly ConditionalWeakTable<LambdaExpression, HashSet<string>> _ignoredMembersByMap = new();

    private static readonly ConcurrentDictionary<Tuple<Type, Type, string>, bool> _initializedPropertyCache = new();

    private static bool IsParameterMarker(MethodInfo method) {
        if (!method.IsGenericMethod || method.DeclaringType != typeof(MapifyProfile)) {
            return false;
        }

        var genericDefinition = method.GetGenericMethodDefinition();
        return string.Equals(genericDefinition.Name, _parameterMarkerName, StringComparison.Ordinal)
            && genericDefinition.GetGenericArguments().Length == 1
            && genericDefinition.GetParameters().Length == 1
            && genericDefinition.GetParameters()[0].ParameterType == typeof(string);
    }

    private static Action<TSource, TTarget> CompileMapper<TSource, TTarget>(Expression<Func<TSource, TTarget>> expression) {
        _ignoredMembersByMap.TryGetValue(expression, out HashSet<string>? ignoredMembers);

        if (expression.Body is not MemberInitExpression initExpr)
            throw new ArgumentException("Expression must be a member initializer (new TTarget { ... })");

        var sourceParam = Expression.Parameter(typeof(TSource), "src");
        var targetParam = Expression.Parameter(typeof(TTarget), "target");
        var assignments = new List<Expression>();

        foreach (var binding in initExpr.Bindings) {
            if (binding is not MemberAssignment ma)
                throw new NotSupportedException("Only member assignments are supported");

            if (ignoredMembers != null && ignoredMembers.Contains(ma.Member.Name)) {
                continue;
            }

            var replaced = new ParameterReplaceVisitor(expression.Parameters[0], sourceParam).Visit(ma.Expression);
            var assign = Expression.Assign(
                Expression.PropertyOrField(targetParam, ma.Member.Name),
                replaced
            );

            assignments.Add(assign);
        }

        var block = Expression.Block(assignments);
        var action = Expression.Lambda<Action<TSource, TTarget>>(block, sourceParam, targetParam).Compile();

        return action;
    }

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
                .Visit(partial.Body);

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
            if (existingBindings.ContainsKey(destProp.Name) || ignoredBindings.Contains(destProp.Name))
                continue;

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

    private static bool TryResolveIgnoreMarker(
        MemberInfo destinationMember,
        Expression destinationExpression,
        Type destinationType,
        out MemberAssignment? mappedBinding
    ) {
        mappedBinding = null;

        var markerCandidate = UnwrapConvert(destinationExpression);
        if (markerCandidate is not MethodCallExpression methodCall) {
            return false;
        }

        if (!IsIgnoreMarker(methodCall.Method)) {
            return false;
        }

        if (methodCall.Arguments.Count != 0) {
            throw new InvalidOperationException($"{_ignoreMarkerName} does not accept arguments. Use {_ignoreMarkerName}<T>() to ignore a destination property.");
        }

        if (destinationMember is not PropertyInfo destProp) {
            throw new InvalidOperationException($"{_ignoreMarkerName} marker can only be used for property bindings.");
        }

        if (!IsRequiredMember(destProp)) {
            return true;
        }

        if (IsPropertyInitializedOnFreshInstance(destProp, destinationType)) {
            return true;
        }

        mappedBinding = Expression.Bind(destProp, CreatePropertyDefaultValueExpression(destProp));
        return true;
    }

    private sealed class UseMapMarkerReplaceVisitor(
        Func<Type, Type, string?, LambdaExpression?> existingMapResolver
    ) : ExpressionVisitor {
        protected override Expression VisitMethodCall(MethodCallExpression node) {
            if (IsUseMapMarker(node.Method)) {
                if (!TryResolveUseMapCall(node, existingMapResolver, out var replacement)) {
                    var genericArgs = node.Method.GetGenericArguments();
                    throw new InvalidOperationException($"No mapping found for {genericArgs[0].FullName} -> {genericArgs[1].FullName} required by {_useMapMarkerName}.");
                }

                return replacement;
            }

            if (IsProjectToMarker(node.Method)) {
                if (!TryResolveProjectToCall(node, existingMapResolver, out var replacement)) {
                    throw new InvalidOperationException($"No mapping found for nested {_projectToMarkerName} call from '{node.Arguments[0].Type.FullName}' to '{node.Type.FullName}'.");
                }

                return replacement;
            }

            if (IsIgnoreMarker(node.Method)) {
                throw new InvalidOperationException($"{_ignoreMarkerName} can only be used as a direct destination property binding (e.g. Property = {_ignoreMarkerName}<T>()).");
            }

            return base.VisitMethodCall(node);
        }
    }

    private static bool TryResolveProjectToCall(
        MethodCallExpression methodCall,
        Func<Type, Type, string?, LambdaExpression?> existingMapResolver,
        out Expression resolvedExpression
    ) {
        resolvedExpression = null!;

        string? markerMapName = null;
        Expression sourceAccess;

        if (methodCall.Arguments.Count == 1) {
            sourceAccess = methodCall.Arguments[0];
        } else if (methodCall.Arguments.Count == 2) {
            if (methodCall.Arguments[1].Type == typeof(bool)) {
                sourceAccess = methodCall.Arguments[0];
            } else {
                if (methodCall.Arguments[1] is not ConstantExpression nameConstant || nameConstant.Value is not string mapName || string.IsNullOrWhiteSpace(mapName)) {
                    throw new InvalidOperationException($"{_projectToMarkerName} name argument must be a non-empty constant string.");
                }

                markerMapName = mapName;
                sourceAccess = methodCall.Arguments[0];
            }
        } else {
            return false;
        }

        if (!TryBuildMappedExpression(sourceAccess, sourceAccess.Type, methodCall.Type, existingMapResolver, markerMapName, out var mappedBody, out var sourceNullCheck)) {
            return false;
        }

        if (sourceNullCheck != null && !IsCollectionLikeType(methodCall.Type)) {
            mappedBody = Expression.Condition(
                sourceNullCheck,
                mappedBody,
                CreateDefaultValueExpression(methodCall.Type)
            );
        }

        resolvedExpression = mappedBody;
        return true;
    }

    private static bool TryResolveUseMapCall(
        MethodCallExpression methodCall,
        Func<Type, Type, string?, LambdaExpression?> existingMapResolver,
        out Expression resolvedExpression
    ) {
        resolvedExpression = null!;

        var genericArgs = methodCall.Method.GetGenericArguments();
        var markerSourceType = genericArgs[0];
        var markerTargetType = genericArgs[1];

        if (!TryParseUseMapMarkerArguments(methodCall, out _, out _, out var markerMapName, out var sourceAccess)) {
            throw new InvalidOperationException($"{_useMapMarkerName} requires an explicit source argument. Use {_useMapMarkerName}<TSource, TTarget>(x.Property). For same-name properties you can omit {_useMapMarkerName} and rely on implicit nested map resolution.");
        }

        if (!TryBuildMappedExpression(sourceAccess, markerSourceType, markerTargetType, existingMapResolver, markerMapName, out var mappedBody, out var sourceNullCheck)) {
            return false;
        }

        if (!TryAdaptMappedResult(mappedBody, markerTargetType, out var adaptedResult)) {
            throw new InvalidOperationException($"{_useMapMarkerName} target type '{markerTargetType.FullName}' is not compatible with resolved map output type '{mappedBody.Type.FullName}'.");
        }

        if (sourceNullCheck != null && !IsCollectionLikeType(methodCall.Type)) {
            adaptedResult = Expression.Condition(
                sourceNullCheck,
                adaptedResult,
                CreateDefaultValueExpression(markerTargetType)
            );
        }

        resolvedExpression = adaptedResult;
        return true;
    }

    private static bool TryResolveUseMapMarker(
        MemberInfo destinationMember,
        Expression destinationExpression,
        Func<Type, Type, string?, LambdaExpression?>? existingMapResolver,
        out MemberAssignment mappedBinding
    ) {
        mappedBinding = null!;

        var markerCandidate = UnwrapConvert(destinationExpression);
        if (markerCandidate is not MethodCallExpression methodCall) {
            return false;
        }

        if (!IsUseMapMarker(methodCall.Method)) {
            return false;
        }

        if (destinationMember is not PropertyInfo destProp) {
            throw new InvalidOperationException($"{_useMapMarkerName} marker can only be used for property bindings.");
        }

        if (existingMapResolver == null) {
            throw new InvalidOperationException($"{_useMapMarkerName} marker requires a map resolver.");
        }

        var genericArgs = methodCall.Method.GetGenericArguments();
        var markerSourceType = genericArgs[0];
        var markerTargetType = genericArgs[1];

        if (!TryParseUseMapMarkerArguments(methodCall, out _, out _, out var markerMapName, out var sourceAccess)) {
            throw new InvalidOperationException($"{_useMapMarkerName} requires an explicit source argument. Use {_useMapMarkerName}<TSource, TTarget>(x.Property). For same-name properties you can omit {_useMapMarkerName} and rely on implicit nested map resolution.");
        }

        if (!TryBuildMappedExpression(sourceAccess, markerSourceType, markerTargetType, existingMapResolver, markerMapName, out var mappedBody, out var sourceNullCheck)) {
            throw new InvalidOperationException($"No mapping found for {markerSourceType.FullName} -> {markerTargetType.FullName} required by {_useMapMarkerName} on property '{destProp.Name}'.");
        }

        if (!TryAdaptMappedResult(mappedBody, destProp.PropertyType, out var adaptedResult)) {
            throw new InvalidOperationException($"{_useMapMarkerName} target type '{destProp.PropertyType.FullName}' is not compatible with map target type '{markerTargetType.FullName}' for property '{destProp.Name}'.");
        }

        if (sourceNullCheck != null) {
            var nullFallback = CreatePropertyDefaultValueExpression(destProp);
            if (nullFallback.Type != adaptedResult.Type && adaptedResult.Type.IsAssignableFrom(nullFallback.Type)) {
                nullFallback = Expression.Convert(nullFallback, adaptedResult.Type);
            }

            adaptedResult = Expression.Condition(sourceNullCheck, adaptedResult, nullFallback);
        }

        mappedBinding = Expression.Bind(destProp, adaptedResult);
        return true;
    }

    private static Expression UnwrapConvert(Expression expression) {
        var current = expression;
        while (current is UnaryExpression unary
               && (unary.NodeType == ExpressionType.Convert || unary.NodeType == ExpressionType.ConvertChecked)) {
            current = unary.Operand;
        }

        return current;
    }

    private static bool IsUseMapMarker(MethodInfo method) {
        if (!method.IsGenericMethod || method.DeclaringType != typeof(MapifyProfile)) {
            return false;
        }

        var genericDefinition = method.GetGenericMethodDefinition();
        if (!string.Equals(genericDefinition.Name, _useMapMarkerName, StringComparison.Ordinal)) {
            return false;
        }

        if (genericDefinition.GetGenericArguments().Length != 2) {
            return false;
        }

        var parameters = genericDefinition.GetParameters();
        return parameters.Length == 1
            || (parameters.Length == 2 && parameters[0].ParameterType == genericDefinition.GetGenericArguments()[0] && parameters[1].ParameterType == typeof(int))
            || (parameters.Length == 2 && parameters[0].ParameterType == typeof(string))
            || (parameters.Length == 3 && parameters[0].ParameterType == typeof(string) && parameters[2].ParameterType == typeof(int));
    }

    private static bool TryParseUseMapMarkerArguments(
        MethodCallExpression methodCall,
        out Type markerSourceType,
        out Type markerTargetType,
        out string? markerMapName,
        out Expression sourceAccess
    ) {
        markerMapName = null;
        sourceAccess = null!;

        var genericArgs = methodCall.Method.GetGenericArguments();
        markerSourceType = genericArgs[0];
        markerTargetType = genericArgs[1];

        var args = methodCall.Arguments;
        var genericDefinition = methodCall.Method.GetGenericMethodDefinition();
        var definitionGenericArgs = genericDefinition.GetGenericArguments();
        var sourceGenericParameter = definitionGenericArgs[0];
        var parameterTypes = genericDefinition.GetParameters().Select(parameter => parameter.ParameterType).ToArray();

        if (parameterTypes.Length == 1 && parameterTypes[0] == sourceGenericParameter && args.Count == 1) {
            sourceAccess = args[0];
            return true;
        }

        if (parameterTypes.Length == 2
            && parameterTypes[0] == sourceGenericParameter
            && parameterTypes[1] == typeof(int)
            && args.Count == 2) {
            if (args[1] is not ConstantExpression depthConstant || depthConstant.Value is not int depth || depth <= 0) {
                throw new InvalidOperationException($"{_useMapMarkerName} depth argument must be a constant positive integer.");
            }

            sourceAccess = args[0];
            return true;
        }

        if (parameterTypes.Length == 2
            && parameterTypes[0] == typeof(string)
            && parameterTypes[1] == sourceGenericParameter
            && args.Count == 2) {
            if (args[0] is not ConstantExpression nameConstant || nameConstant.Value is not string mapName || string.IsNullOrWhiteSpace(mapName)) {
                throw new InvalidOperationException($"{_useMapMarkerName} name argument must be a non-empty constant string.");
            }

            markerMapName = mapName;
            sourceAccess = args[1];
            return true;
        }

        if (parameterTypes.Length == 3
            && parameterTypes[0] == typeof(string)
            && parameterTypes[1] == sourceGenericParameter
            && parameterTypes[2] == typeof(int)
            && args.Count == 3) {
            if (args[0] is not ConstantExpression nameConstant || nameConstant.Value is not string mapName || string.IsNullOrWhiteSpace(mapName)) {
                throw new InvalidOperationException($"{_useMapMarkerName} name argument must be a non-empty constant string.");
            }

            if (args[2] is not ConstantExpression depthConstant || depthConstant.Value is not int depth || depth <= 0) {
                throw new InvalidOperationException($"{_useMapMarkerName} depth argument must be a constant positive integer.");
            }

            markerMapName = mapName;
            sourceAccess = args[1];
            return true;
        }

        return false;
    }

    private static bool IsIgnoreMarker(MethodInfo method) {
        if (!method.IsGenericMethod || method.DeclaringType != typeof(MapifyProfile)) {
            return false;
        }

        var genericDefinition = method.GetGenericMethodDefinition();
        if (!string.Equals(genericDefinition.Name, _ignoreMarkerName, StringComparison.Ordinal)) {
            return false;
        }

        return genericDefinition.GetGenericArguments().Length == 1
            && genericDefinition.GetParameters().Length == 0;
    }

    private static bool IsRequiredMember(MemberInfo member)
        => member.CustomAttributes.Any(x => string.Equals(x.AttributeType.FullName, "System.Runtime.CompilerServices.RequiredMemberAttribute", StringComparison.Ordinal));

    private static bool IsProjectToMarker(MethodInfo method) {
        if (!method.IsGenericMethod || method.DeclaringType != typeof(MapifyProjectToExtensions)) {
            return false;
        }

        var genericDefinition = method.GetGenericMethodDefinition();
        if (!string.Equals(genericDefinition.Name, _projectToMarkerName, StringComparison.Ordinal)) {
            return false;
        }

        if (genericDefinition.GetGenericArguments().Length != 1) {
            return false;
        }

        var parameters = genericDefinition.GetParameters();
        if (parameters.Length < 1 || parameters.Length > 2) {
            return false;
        }

        if (!typeof(System.Collections.IEnumerable).IsAssignableFrom(parameters[0].ParameterType)
            && !typeof(IQueryable).IsAssignableFrom(parameters[0].ParameterType)) {
            return false;
        }

        if (parameters.Length == 2 && parameters[1].ParameterType != typeof(string)) {
            return parameters[1].ParameterType == typeof(bool);
        }

        return true;
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

    private static Expression? BuildNestedMemberAccessGuard(Expression expression) {
        var collector = new NestedMemberAccessNullGuardCollector();
        collector.Visit(expression);
        return collector.BuildGuard();
    }

    private static Expression ApplyNestedNullSafety(Expression expression, PropertyInfo? destinationProperty) {
        expression = new LambdaBodyNullSafetyRewriter().Visit(expression)!;
        var fallback = CreateAssignmentFallbackExpression(expression.Type, destinationProperty);
        return ApplyNestedNullSafetyCore(expression, fallback);
    }

    private static Expression ApplyNestedNullSafetyCore(Expression expression, Expression fallback) {
        if (expression is BinaryExpression binaryExpression && binaryExpression.NodeType == ExpressionType.Coalesce) {
            var normalized = NormalizeCoalesceToConditional(binaryExpression);
            return ApplyNestedNullSafetyCore(normalized, fallback);
        }

        if (expression is ConditionalExpression conditionalExpression) {
            var guardedTest = ApplyNestedNullSafetyToBooleanExpression(conditionalExpression.Test);
            var guardedIfTrue = ApplyNestedNullSafetyCore(
                conditionalExpression.IfTrue,
                AdaptFallbackToType(fallback, conditionalExpression.IfTrue.Type)
            );
            var guardedIfFalse = ApplyNestedNullSafetyCore(
                conditionalExpression.IfFalse,
                AdaptFallbackToType(fallback, conditionalExpression.IfFalse.Type)
            );

            return Expression.Condition(guardedTest, guardedIfTrue, guardedIfFalse);
        }

        var guard = BuildNestedMemberAccessGuard(expression);
        if (guard == null) {
            return expression;
        }

        var guardedFallback = expression.Type == typeof(bool)
            ? CreateBooleanNullFallback(expression)
            : AdaptFallbackToType(fallback, expression.Type);

        return Expression.Condition(guard, expression, guardedFallback);
    }

    private static Expression ApplyNestedNullSafetyToBooleanExpression(Expression testExpression) {
        if (testExpression is ConditionalExpression conditionalExpression) {
            var guardedTest = ApplyNestedNullSafetyToBooleanExpression(conditionalExpression.Test);
            var guardedIfTrue = ApplyNestedNullSafetyToBooleanExpression(conditionalExpression.IfTrue);
            var guardedIfFalse = ApplyNestedNullSafetyToBooleanExpression(conditionalExpression.IfFalse);
            return Expression.Condition(guardedTest, guardedIfTrue, guardedIfFalse);
        }

        var guard = BuildNestedMemberAccessGuard(testExpression);
        if (guard == null) {
            return testExpression;
        }

        return Expression.Condition(guard, testExpression, CreateBooleanNullFallback(testExpression));
    }

    private static Expression CreateBooleanNullFallback(Expression booleanExpression) {
        if (booleanExpression is BinaryExpression binaryExpression
            && (binaryExpression.NodeType == ExpressionType.Equal || binaryExpression.NodeType == ExpressionType.NotEqual)) {
            var leftIsNull = IsNullConstant(binaryExpression.Left);
            var rightIsNull = IsNullConstant(binaryExpression.Right);

            if (leftIsNull ^ rightIsNull) {
                var comparedExpression = leftIsNull ? binaryExpression.Right : binaryExpression.Left;
                if (CanBeNull(comparedExpression.Type)) {
                    return Expression.Constant(binaryExpression.NodeType == ExpressionType.Equal);
                }
            }
        }

        return Expression.Constant(false);
    }

    private static bool IsNullConstant(Expression expression)
        => expression is ConstantExpression constantExpression && constantExpression.Value == null;

    private static Expression CreateAssignmentFallbackExpression(Type targetType, PropertyInfo? destinationProperty) {
        var fallback = destinationProperty != null
            ? CreatePropertyDefaultValueExpression(destinationProperty)
            : CreateDefaultValueExpression(targetType);

        return AdaptFallbackToType(fallback, targetType);
    }

    private static Expression AdaptFallbackToType(Expression fallback, Type targetType) {
        if (fallback.Type == targetType) {
            return fallback;
        }

        if (TryAdaptMappedResult(fallback, targetType, out var adapted)) {
            return adapted;
        }

        return CreateDefaultValueExpression(targetType);
    }

    private static Expression NormalizeCoalesceToConditional(BinaryExpression coalesceExpression) {
        var test = Expression.NotEqual(coalesceExpression.Left, Expression.Constant(null, coalesceExpression.Left.Type));
        var value = Nullable.GetUnderlyingType(coalesceExpression.Left.Type) != null
             && (coalesceExpression.Right is not ConstantExpression rightConst || rightConst.Value != null)
            ? Expression.Property(coalesceExpression.Left, "Value")
            : coalesceExpression.Left;

        return Expression.Condition(test, value, coalesceExpression.Right);
    }

    private sealed class LambdaBodyNullSafetyRewriter : ExpressionVisitor {
        protected override Expression VisitLambda<T>(Expression<T> node) {
            var guardedBody = ApplyNestedNullSafetyCore(node.Body, CreateDefaultValueExpression(node.Body.Type));
            return Expression.Lambda<T>(guardedBody, node.Parameters);
        }
    }

    private sealed class NestedMemberAccessNullGuardCollector : ExpressionVisitor {
        private readonly List<Expression> _checks = [];

        public Expression? BuildGuard() {
            if (_checks.Count == 0) {
                return null;
            }

            Expression combined = _checks[0];
            for (var i = 1; i < _checks.Count; i++) {
                combined = Expression.AndAlso(combined, _checks[i]);
            }

            return combined;
        }

        protected override Expression VisitMember(MemberExpression node) {
            if (node.Expression != null) {
                Visit(node.Expression);

                if (RequiresNullCheck(node.Expression) && !IsNullableHasValueAccess(node)) {
                    _checks.Add(CreateHasValueCheck(node.Expression));
                }
            }

            return node;
        }

        protected override Expression VisitMethodCall(MethodCallExpression node) {
            if (node.Object != null) {
                Visit(node.Object);

                if (RequiresNullCheck(node.Object) && !IsSafeNullableMethodCall(node)) {
                    _checks.Add(CreateHasValueCheck(node.Object));
                }
            }

            foreach (var argument in node.Arguments) {
                Visit(argument);
            }

            return node;
        }

        protected override Expression VisitLambda<T>(Expression<T> node)
            => node;

        private static bool IsNullableHasValueAccess(MemberExpression node) {
            if (node.Member.Name != "HasValue") {
                return false;
            }

            var nullableCarrierType = node.Member.DeclaringType ?? node.Expression?.Type;
            return nullableCarrierType != null && Nullable.GetUnderlyingType(nullableCarrierType) != null;
        }

        private static bool IsSafeNullableMethodCall(MethodCallExpression node) {
            if (node.Object == null) {
                return false;
            }

            if (Nullable.GetUnderlyingType(node.Object.Type) == null) {
                return false;
            }

            if (node.Method.Name == nameof(Nullable<int>.GetValueOrDefault)) {
                return node.Arguments.Count == 0 || node.Arguments.Count == 1;
            }

            if (node.Method.Name == nameof(ToString)) {
                return node.Arguments.Count == 0;
            }

            if (node.Method.Name == nameof(GetHashCode)) {
                return node.Arguments.Count == 0;
            }

            if (node.Method.Name == nameof(Equals)) {
                return node.Arguments.Count == 1;
            }

            return false;
        }

        private static bool RequiresNullCheck(Expression expression)
            => CanBeNull(expression.Type) && expression is not ParameterExpression;
    }

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

    private static bool IsPropertyInitializedOnFreshInstance(PropertyInfo property, Type destinationType) {
        if (!property.CanRead || property.DeclaringType == null) {
            return false;
        }

        var cacheKey = Tuple.Create(destinationType, property.DeclaringType, property.Name);
        return _initializedPropertyCache.GetOrAdd(cacheKey, _ => {
            try {
                var instance = Activator.CreateInstance(destinationType);
                if (instance == null) {
                    return false;
                }

                var value = property.GetValue(instance);
                return !IsDefaultValue(value, property.PropertyType);
            } catch {
                return false;
            }
        });
    }

    private static bool IsDefaultValue(object? value, Type type) {
        if (value == null) {
            return true;
        }

        if (!type.IsValueType) {
            return false;
        }

        var defaultValue = Activator.CreateInstance(type);
        return Equals(value, defaultValue);
    }

    private static Expression CreatePropertyDefaultValueExpression(PropertyInfo property) {
        if (IsCollectionLikeType(property.PropertyType)
            && ShouldUseEmptyCollectionFallback(property)
            && TryCreateEmptyCollectionExpression(property.PropertyType, out var emptyCollectionExpression)) {
            return emptyCollectionExpression;
        }

        return CreateDefaultValueExpression(property.PropertyType);
    }

    private static bool ShouldUseEmptyCollectionFallback(PropertyInfo property)
        => IsRequiredMember(property) || !IsPropertyDeclaredNullable(property);

    private static bool IsPropertyDeclaredNullable(PropertyInfo property) {
        if (property.PropertyType.IsValueType) {
            return Nullable.GetUnderlyingType(property.PropertyType) != null;
        }

        var nullabilityContextType = Type.GetType("System.Reflection.NullabilityInfoContext");
        if (nullabilityContextType != null) {
            try {
                var nullabilityContext = Activator.CreateInstance(nullabilityContextType);
                var createMethod = nullabilityContextType.GetMethod("Create", [typeof(PropertyInfo)]);
                var nullabilityInfo = createMethod?.Invoke(nullabilityContext, [property]);
                var writeState = nullabilityInfo?.GetType().GetProperty("WriteState")?.GetValue(nullabilityInfo);
                if (writeState != null) {
                    var stateName = writeState.ToString();
                    if (string.Equals(stateName, "Nullable", StringComparison.Ordinal)) {
                        return true;
                    }

                    if (string.Equals(stateName, "NotNull", StringComparison.Ordinal)) {
                        return false;
                    }
                }
            } catch {
            }
        }

        var propertyNullableFlag = TryGetNullableAttributeFlag(property.CustomAttributes);
        if (propertyNullableFlag.HasValue) {
            return propertyNullableFlag.Value == 2;
        }

        var contextNullableFlag = TryGetNullableContextFlag(property);
        if (contextNullableFlag.HasValue) {
            return contextNullableFlag.Value == 2;
        }

        return true;
    }

    private static byte? TryGetNullableAttributeFlag(IEnumerable<CustomAttributeData> attributes) {
        foreach (var attribute in attributes) {
            if (!string.Equals(attribute.AttributeType.FullName, "System.Runtime.CompilerServices.NullableAttribute", StringComparison.Ordinal)) {
                continue;
            }

            if (attribute.ConstructorArguments.Count == 1) {
                var argument = attribute.ConstructorArguments[0];
                if (argument.ArgumentType == typeof(byte)) {
                    return (byte)argument.Value!;
                }

                if (argument.ArgumentType == typeof(byte[])
                    && argument.Value is IReadOnlyCollection<CustomAttributeTypedArgument> values
                    && values.Count > 0) {
                    return (byte)values.First().Value!;
                }
            }
        }

        return null;
    }

    private static byte? TryGetNullableContextFlag(PropertyInfo property) {
        for (Type? currentType = property.DeclaringType; currentType != null; currentType = currentType.DeclaringType) {
            var contextFlag = TryGetNullableContextFlag(currentType.CustomAttributes);
            if (contextFlag.HasValue) {
                return contextFlag;
            }
        }

        return null;
    }

    private static byte? TryGetNullableContextFlag(IEnumerable<CustomAttributeData> attributes) {
        foreach (var attribute in attributes) {
            if (!string.Equals(attribute.AttributeType.FullName, "System.Runtime.CompilerServices.NullableContextAttribute", StringComparison.Ordinal)) {
                continue;
            }

            if (attribute.ConstructorArguments.Count == 1
                && attribute.ConstructorArguments[0].ArgumentType == typeof(byte)) {
                return (byte)attribute.ConstructorArguments[0].Value!;
            }
        }

        return null;
    }

    private static bool TryCreateEmptyCollectionExpression(Type type, out Expression expression) {
        expression = null!;

        if (!IsCollectionLikeType(type) || !TryGetEnumerableElementType(type, out var elementType)) {
            return false;
        }

        if (type.IsArray) {
            expression = Expression.NewArrayInit(elementType);
            return true;
        }

        if (type.IsInterface && type.IsGenericType) {
            var genericDefinition = type.GetGenericTypeDefinition();
            if (genericDefinition == typeof(IEnumerable<>)
                || genericDefinition == typeof(ICollection<>)
                || genericDefinition == typeof(IList<>)
                || genericDefinition == typeof(IReadOnlyCollection<>)
                || genericDefinition == typeof(IReadOnlyList<>)) {
                expression = Expression.New(typeof(List<>).MakeGenericType(elementType));
                return true;
            }
        }

        var parameterlessConstructor = type.GetConstructor(Type.EmptyTypes);
        if (parameterlessConstructor != null) {
            expression = Expression.New(parameterlessConstructor);
            return true;
        }

        var enumerableConstructor = type.GetConstructor([typeof(IEnumerable<>).MakeGenericType(elementType)]);
        if (enumerableConstructor != null) {
            var emptyEnumerable = Expression.Call(
                typeof(Enumerable),
                nameof(Enumerable.Empty),
                [elementType]
            );
            expression = Expression.New(enumerableConstructor, emptyEnumerable);
            return true;
        }

        return false;
    }

    private static Expression CreateDefaultValueExpression(Type type)
        => CanBeNull(type)
            ? Expression.Constant(null, type)
            : Expression.Default(type);

    private static Expression<Func<TSource, TTarget>> ApplyParameters<TSource, TTarget>(
        Expression<Func<TSource, TTarget>> mappingExpression,
        IReadOnlyDictionary<string, object?>? parameters
    ) {
        return (Expression<Func<TSource, TTarget>>)ApplyParameters((LambdaExpression)mappingExpression, parameters);
    }

    private static LambdaExpression ApplyParameters(LambdaExpression mappingExpression, IReadOnlyDictionary<string, object?>? parameters) {
        var replacedBody = new ParameterMarkerReplaceVisitor(parameters).Visit(mappingExpression.Body)!;
        return replacedBody == mappingExpression.Body
            ? mappingExpression
            : Expression.Lambda(replacedBody, mappingExpression.Parameters);
    }

    private static bool ContainsParameterMarkers(LambdaExpression expression)
        => new ParameterMarkerDetector().ContainsMarker(expression);

    private sealed class ParameterMarkerDetector : ExpressionVisitor {
        private bool _containsMarker;

        public bool ContainsMarker(LambdaExpression expression) {
            _containsMarker = false;
            Visit(expression);
            return _containsMarker;
        }

        protected override Expression VisitMethodCall(MethodCallExpression node) {
            if (IsParameterMarker(node.Method)) {
                _containsMarker = true;
                return node;
            }

            return base.VisitMethodCall(node);
        }
    }
}
