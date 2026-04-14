using System.Linq.Expressions;
using System.Reflection;

namespace Mapify.NET;

public partial class Mapify {
    private const string _useMapMarkerName = "UseMap";

    private const string _ignoreMarkerName = "Ignore";

    private const string _projectToMarkerName = "ProjectTo";

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
}
