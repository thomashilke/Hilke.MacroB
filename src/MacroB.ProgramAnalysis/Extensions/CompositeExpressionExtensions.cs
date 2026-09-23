namespace Hilke.MacroB.FrontEnd;

public static class CompositeExpressionExtensions
{
    extension(OperandBase operand)
    {
        /// <summary>
        /// Returns every <see cref="SsaVariable" /> reachable from this operand, recursing into
        /// <see cref="CompositeUnaryExpression" />/<see cref="CompositeBinaryExpression" /> trees.
        /// <see cref="ExpressionRebuildTransform" /> replaces a folded variable's use site with a
        /// composite expression containing that variable (and any of its own operands) nested
        /// inside; every variable-usage scan (register allocation, liveness) must use this instead
        /// of a shallow `is SsaVariable`/`OfType&lt;SsaVariable&gt;()` check so folded variables stay
        /// visible.
        /// </summary>
        public IEnumerable<SsaVariable> GetReferencedVariables()
        {
            return operand switch
            {
                SsaVariable variable => new[] { variable },
                CompositeUnaryExpression unary => unary.Expression.GetReferencedVariables(),
                CompositeBinaryExpression binary => binary.Left.GetReferencedVariables()
                                                           .Concat(binary.Right.GetReferencedVariables()),
                _ => Enumerable.Empty<SsaVariable>()
            };
        }
    }
}
