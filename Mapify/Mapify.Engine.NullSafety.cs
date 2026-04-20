using System.Linq.Expressions;
using System.Reflection;

namespace Mapify.NET;

public partial class Mapify {
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

            return Expression.Condition(guardedTest, guardedIfTrue, guardedIfFalse, conditionalExpression.Type);
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
            var visitedBody = Visit(node.Body)!;

            if (!node.ReturnType.IsValueType && (visitedBody is MemberInitExpression || visitedBody is NewExpression)) {
                return Expression.Lambda<T>(visitedBody, node.Parameters);
            }

            var guardedBody = ApplyNestedNullSafetyCore(visitedBody, CreateDefaultValueExpression(visitedBody.Type));
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
}
