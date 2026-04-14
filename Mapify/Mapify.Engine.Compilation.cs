using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace Mapify.NET;

public partial class Mapify {
    private static readonly ConditionalWeakTable<LambdaExpression, HashSet<string>> _ignoredMembersByMap = new();

    private static Action<TSource, TTarget> CompileMapper<TSource, TTarget>(Expression<Func<TSource, TTarget>> expression) {
        _ignoredMembersByMap.TryGetValue(expression, out HashSet<string>? ignoredMembers);

        if (expression.Body is not MemberInitExpression initExpr) {
            throw new ArgumentException("Expression must be a member initializer (new TTarget { ... })");
        }

        var sourceParam = Expression.Parameter(typeof(TSource), "src");
        var targetParam = Expression.Parameter(typeof(TTarget), "target");
        var assignments = new List<Expression>();

        foreach (var binding in initExpr.Bindings) {
            if (binding is not MemberAssignment ma) {
                throw new NotSupportedException("Only member assignments are supported");
            }

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
}
