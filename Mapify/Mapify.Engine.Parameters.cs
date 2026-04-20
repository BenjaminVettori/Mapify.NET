using System.Linq.Expressions;
using System.Reflection;

namespace Mapify.NET;

public partial class Mapify {
    private const string _parameterMarkerName = "Parameter";

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
