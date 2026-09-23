namespace Hilke.MacroB.FrontEnd;

/// <summary>
/// Rebuilds complex mathematical expressions by inlining single-use, single-block temporary
/// variables directly into the instruction that consumes them, replacing the temporary's
/// register with a <see cref="CompositeUnaryExpression" />/<see cref="CompositeBinaryExpression" />
/// tree. This lets <see cref="MacroBCodeGenerator" /> render one bracketed, precedence-correct
/// expression (e.g. "SIN[[#1+#2]*2]") instead of one Macro B register assignment per
/// sub-expression.
/// </summary>
public class ExpressionRebuildTransform : IControlFlowGraphTransformation<TacInstructionBlock>
{
    // Operators MacroBCodeGenerator can render as a nested composite today: the four infix
    // arithmetic operators plus the two comparisons actually produced by TacConverter, and the
    // unary math functions that already have complete, correct "NAME[arg]" rendering. Operators
    // whose own rendering is incomplete or ambiguous (Pow, Adp, Rem: arity mismatch/unimplemented
    // in ConstantPropagatorTransform) or missing from MacroBCodeGenerator's symbol table
    // (Ceq, Or, XOr, And, Not, Neg) are deliberately excluded so this pass never folds a variable
    // into an expression shape the generator cannot already render correctly on its own. None of
    // the excluded operators are produced by TacConverter today anyway.
    private static readonly HashSet<Operand> FoldableOperators = new()
    {
        Operand.Add, Operand.Sub, Operand.Mul, Operand.Div, Operand.Cgt, Operand.Clt,
        Operand.Sin, Operand.Cos, Operand.Tan, Operand.ASin, Operand.ACos, Operand.ATan,
        Operand.Sqrt, Operand.Abs, Operand.Bin, Operand.Bcd, Operand.Round, Operand.Fix,
        Operand.Fup, Operand.Ln, Operand.Exp
    };

    /// <inheritdoc />
    public ControlFlowGraph<TacInstructionBlock> Transform(ControlFlowGraph<TacInstructionBlock> controlFlowGraph)
    {
        var blockOf = new Dictionary<TacInstruction, TacInstructionBlock>();
        var definitions = new Dictionary<SsaVariable, TacInstruction>();
        var useCounts = new Dictionary<SsaVariable, int>();
        var soleUse = new Dictionary<SsaVariable, (TacInstruction Instruction, int ArgumentIndex)>();

        foreach (var block in controlFlowGraph.Blocks)
        {
            foreach (var instruction in block.Instructions)
            {
                blockOf[instruction] = block;

                if (instruction.Destination is { } destination)
                {
                    definitions[destination] = instruction;
                }

                for (var argumentIndex = 0; argumentIndex < instruction.Arguments.Count; ++argumentIndex)
                {
                    if (instruction.Arguments[argumentIndex] is SsaVariable used)
                    {
                        useCounts[used] = useCounts.GetValueOrDefault(used) + 1;
                        soleUse[used] = (instruction, argumentIndex);
                    }
                }
            }
        }

        bool IsFoldable(SsaVariable variable)
        {
            return useCounts.GetValueOrDefault(variable) == 1
                && definitions.TryGetValue(variable, out var definition)
                && FoldableOperators.Contains(definition.Op)
                && soleUse[variable].Instruction.Op != Operand.Phi
                && blockOf[definition] == blockOf[soleUse[variable].Instruction];
        }

        var built = new Dictionary<SsaVariable, OperandBase>();

        OperandBase Resolve(OperandBase argument)
        {
            return argument is SsaVariable variable && IsFoldable(variable)
                       ? Build(variable)
                       : argument;
        }

        OperandBase Build(SsaVariable variable)
        {
            if (built.TryGetValue(variable, out var cached))
            {
                return cached;
            }

            var definition = definitions[variable];

            OperandBase composite = definition.Arguments.Count switch
            {
                1 => new CompositeUnaryExpression(Resolve(definition.Arguments[0]), definition.Op),
                2 => new CompositeBinaryExpression(
                    Resolve(definition.Arguments[0]), definition.Op, Resolve(definition.Arguments[1])),
                _ => throw new InvalidOperationException(
                    $"Foldable operator {definition.Op} must have exactly 1 or 2 arguments.")
            };

            built[variable] = composite;
            blockOf[definition].RemoveInstruction(definition);

            return composite;
        }

        foreach (var variable in definitions.Keys.Where(IsFoldable).ToList())
        {
            var (useInstruction, argumentIndex) = soleUse[variable];
            useInstruction.Arguments[argumentIndex] = Build(variable);
        }

        return controlFlowGraph;
    }
}
