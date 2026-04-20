using System.Linq.Expressions;

namespace Mapify.NET;

public partial class Mapify {
    private readonly struct MapKey(Type sourceType, Type targetType, string? name) : IEquatable<MapKey> {
        public Type SourceType { get; } = sourceType;

        public Type TargetType { get; } = targetType;

        public string? Name { get; } = name;

        public bool Equals(MapKey other)
            => SourceType == other.SourceType
               && TargetType == other.TargetType
               && string.Equals(Name, other.Name, StringComparison.Ordinal);

        public override bool Equals(object? obj)
            => obj is MapKey other && Equals(other);

        public override int GetHashCode() {
            unchecked {
                var hash = 17;
                hash = (hash * 23) + SourceType.GetHashCode();
                hash = (hash * 23) + TargetType.GetHashCode();
                hash = (hash * 23) + (Name != null ? StringComparer.Ordinal.GetHashCode(Name) : 0);
                return hash;
            }
        }
    }

    private sealed class PendingMapRegistration(LambdaExpression? partial) {
        public LambdaExpression? Partial { get; } = partial;

        public List<MapBuilderBinding> Bindings { get; } = [];

        public void AddBinding(LambdaExpression targetExpression, LambdaExpression sourceExpression) {
            Bindings.Add(new MapBuilderBinding(targetExpression, sourceExpression));
        }
    }

    private sealed class MapBuilderBinding(LambdaExpression targetExpression, LambdaExpression sourceExpression) {
        public LambdaExpression TargetExpression { get; } = targetExpression;

        public LambdaExpression SourceExpression { get; } = sourceExpression;
    }

    private enum MapBuildState {
        NotBuilt = 0,
        Building = 1,
        Built = 2
    }

    private sealed class UseMapDepthMarkerVisitor : ExpressionVisitor {
        public bool HasUseMapMarkers { get; private set; }

        public bool HasDepthlessUseMapMarkers { get; private set; }

        public int MaxExplicitDepth { get; private set; }

        public static UseMapDepthMarkerVisitor Extract(LambdaExpression expression) {
            var visitor = new UseMapDepthMarkerVisitor();
            visitor.Visit(expression.Body);
            return visitor;
        }

        protected override Expression VisitMethodCall(MethodCallExpression node) {
            if (IsUseMapMarker(node.Method)) {
                HasUseMapMarkers = true;

                var methodDefinition = node.Method.GetGenericMethodDefinition();
                var parameters = methodDefinition.GetParameters();

                if (parameters.Length == 1
                    || (parameters.Length == 2 && parameters[0].ParameterType == typeof(string))) {
                    HasDepthlessUseMapMarkers = true;
                }

                if (parameters.Length == 2 && parameters[1].ParameterType == typeof(int)) {
                    MaxExplicitDepth = Math.Max(MaxExplicitDepth, ExtractDepth(node.Arguments[1]));
                }

                if (parameters.Length == 3 && parameters[2].ParameterType == typeof(int)) {
                    MaxExplicitDepth = Math.Max(MaxExplicitDepth, ExtractDepth(node.Arguments[2]));
                }
            }

            return base.VisitMethodCall(node);
        }

        private static int ExtractDepth(Expression expression) {
            if (expression is ConstantExpression constant && constant.Value is int depth && depth > 0) {
                return depth;
            }

            throw new InvalidOperationException("UseMap depth argument must be a constant positive integer.");
        }
    }
}
