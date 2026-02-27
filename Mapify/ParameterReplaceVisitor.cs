using System.Linq.Expressions;

namespace Mapify.NET; 
/// <summary>
/// This visitor can be used to replace all occurences of a parameter expression in a given Expression recursively
/// </summary>
/// <param name="oldParam">The parameter expression to be replaced</param>
/// <param name="newExpr">The new expression to be used instead</param>
internal class ParameterReplaceVisitor(ParameterExpression oldParam, Expression newExpr) : ExpressionVisitor {
    private readonly ParameterExpression _oldParam = oldParam;
    private readonly Expression _newExpr = newExpr;

    protected override Expression VisitParameter(ParameterExpression node) {
        return node == _oldParam ? _newExpr : base.VisitParameter(node);
    }
}
