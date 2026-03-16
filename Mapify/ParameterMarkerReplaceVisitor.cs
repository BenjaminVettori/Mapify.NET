using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;

namespace Mapify.NET;

internal sealed class ParameterMarkerReplaceVisitor(IReadOnlyDictionary<string, object?>? parameters) : ExpressionVisitor {
    private const string _parameterMarkerName = "Parameter";

    private readonly IReadOnlyDictionary<string, object?>? _parameters = parameters;

    protected override Expression VisitMethodCall(MethodCallExpression node) {
        if (!IsParameterMarker(node.Method)) {
            return base.VisitMethodCall(node);
        }

        if (node.Arguments.Count != 1) {
            throw new InvalidOperationException($"{_parameterMarkerName} requires exactly one parameter name argument.");
        }

        if (node.Arguments[0] is not ConstantExpression parameterNameExpression
            || parameterNameExpression.Value is not string parameterName
            || string.IsNullOrWhiteSpace(parameterName)) {
            throw new InvalidOperationException($"{_parameterMarkerName} name argument must be a non-empty constant string.");
        }

        if (_parameters == null || !_parameters.TryGetValue(parameterName, out var parameterValue)) {
            throw new KeyNotFoundException($"Missing mapping parameter '{parameterName}'. Provide this parameter when calling GetMap/GetRequiredMap/Map/ProjectTo.");
        }

        var targetType = node.Method.GetGenericArguments()[0];
        var normalizedValue = ConvertParameterValue(parameterName, parameterValue, targetType);

        if (normalizedValue == null) {
            return Expression.Constant(null, targetType);
        }

        var valueExpression = Expression.Constant(normalizedValue, normalizedValue.GetType());
        if (valueExpression.Type == targetType) {
            return valueExpression;
        }

        return Expression.Convert(valueExpression, targetType);
    }

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

    private static object? ConvertParameterValue(string parameterName, object? parameterValue, Type targetType) {
        var nonNullableTargetType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (parameterValue == null) {
            if (nonNullableTargetType.IsValueType && Nullable.GetUnderlyingType(targetType) == null) {
                throw new InvalidOperationException($"Mapping parameter '{parameterName}' cannot be null because it is used as non-nullable type '{targetType.FullName}'.");
            }

            return null;
        }

        if (targetType.IsInstanceOfType(parameterValue)) {
            return parameterValue;
        }

        if (nonNullableTargetType.IsInstanceOfType(parameterValue)) {
            return parameterValue;
        }

        try {
            if (nonNullableTargetType.IsEnum) {
                if (parameterValue is string enumName) {
                    return Enum.Parse(nonNullableTargetType, enumName, ignoreCase: true);
                }

                var enumUnderlyingType = Enum.GetUnderlyingType(nonNullableTargetType);
                var numericValue = Convert.ChangeType(parameterValue, enumUnderlyingType, CultureInfo.InvariantCulture);
                return Enum.ToObject(nonNullableTargetType, numericValue!);
            }

            return Convert.ChangeType(parameterValue, nonNullableTargetType, CultureInfo.InvariantCulture);
        } catch (Exception ex) {
            throw new InvalidOperationException($"Mapping parameter '{parameterName}' with runtime type '{parameterValue.GetType().FullName}' cannot be converted to required type '{targetType.FullName}'.", ex);
        }
    }
}
