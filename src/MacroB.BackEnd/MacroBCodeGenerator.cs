using System.Diagnostics;
using System.Text;

namespace Hilke.MacroB.FrontEnd;

public class MacroBCodeGenerator
{
    // Moved out of GenerateCode(TacInstructionBlock,...) (was a per-call local) so it can also be
    // used by RenderInfixExpression for nested composite expressions. Same original 12 entries plus
    // Ceq/Or/XOr/And (previously missing — see fix #2 above).
    private static readonly Dictionary<Operand, string> _binaryOperatorSymbols = new()
    {
        { Operand.Add, " + " },
        { Operand.Sub, " - " },
        { Operand.Mul, " * " },
        { Operand.Div, " / " },
        { Operand.Cgt, " GT " },
        { Operand.Clt, " LT " },
        { Operand.Ble, " LE " },
        { Operand.Blt, " LT " },
        { Operand.Bge, " GE " },
        { Operand.Bgt, " GT " },
        { Operand.Beq, " EQ " },
        { Operand.Bne, " NE " },
        { Operand.Ceq, " EQ " },
        { Operand.Or, " OR " },
        { Operand.XOr, " XOR " },
        { Operand.And, " AND " }
    };

    // Extracted from the previous per-case "$"{NAME}[{...}]"" bodies in GenerateCode(TacInstructionBlock,...);
    // same names/casing as before (including the pre-existing "Ln" casing) so output is unchanged
    // for every operator that already worked. Pow removed — it is binary, not unary (see fix #3
    // above), and now has its own dedicated rendering via RenderPowExpression.
    private static readonly Dictionary<Operand, string> _functionPrefixNames = new()
    {
        { Operand.Sin, "SIN" },
        { Operand.Cos, "COS" },
        { Operand.Tan, "TAN" },
        { Operand.ASin, "ASIN" },
        { Operand.ACos, "ACOS" },
        { Operand.ATan, "ATAN" },
        { Operand.Sqrt, "SQRT" },
        { Operand.Abs, "ABS" },
        { Operand.Bin, "BIN" },
        { Operand.Bcd, "BCD" },
        { Operand.Round, "ROUND" },
        { Operand.Fix, "FIX" },
        { Operand.Fup, "FUP" },
        { Operand.Ln, "Ln" },
        { Operand.Exp, "EXP" },
        { Operand.Adp, "ADP" },
        { Operand.Rem, "REM" },
        { Operand.Neg, "-" }
    };

    // FANUC Custom Macro B priority of operations: functions highest, then * / AND, then + - OR XOR
    // (confirmed against the FANUC Custom Macro B manual). Relational comparisons (GT/LT/EQ/...) are
    // not part of that documented arithmetic chain and only ever appear as a whole IF[...] condition;
    // they are given the lowest tier here purely so an arithmetic child is never spuriously bracketed
    // inside a comparison, and so a comparison nested inside another comparison (only possible via the
    // "<condition> EQ 0" branch-negation text below) still gets a correct, conservative answer.
    private static readonly Dictionary<Operand, int> _binaryOperatorPrecedence = new()
    {
        { Operand.Mul, 2 },
        { Operand.Div, 2 },
        { Operand.And, 2 },
        { Operand.Add, 1 },
        { Operand.Sub, 1 },
        { Operand.Or, 1 },
        { Operand.XOr, 1 },
        { Operand.Cgt, 0 },
        { Operand.Clt, 0 },
        { Operand.Ceq, 0 }
    };

    // Operators for which swapping/regrouping operands changes the result: a same-precedence-tier
    // operator nested as the RIGHT child must stay bracketed (a-(b-c) != a-b-c), unlike Add/Mul which
    // are associative and never need that bracket. Ceq is treated the same as Cgt/Clt (conservative —
    // chained equality comparisons are not documented FANUC behavior). Or/XOr/And are genuine
    // boolean-algebra associative/commutative operators and are intentionally not listed here.
    private static readonly HashSet<Operand> _nonAssociativeBinaryOperators = new()
    {
        Operand.Sub, Operand.Div, Operand.Cgt, Operand.Clt, Operand.Ceq
    };

    private readonly Dictionary<SsaVariable, int> _registerMap;
    private readonly Dictionary<TacInstructionBlock, int> _sequenceNumberMap;
    private readonly Dictionary<int, TacInstructionBlock> _blockJumpTargetMap;
    private readonly BasicBlockOrderAnalysis _blockOrderAnalysis;
    private readonly MacroVariableConfiguration _configuration;
    private readonly IReadOnlyDictionary<string, NcStatementTemplate> _deviceIntrinsics;
    private readonly int? _returnValueSlot;

    public MacroBCodeGenerator(
        ControlFlowGraph<TacInstructionBlock> controlFlowGraph,
        MacroVariableConfiguration configuration,
        IReadOnlyDictionary<string, NcStatementTemplate> deviceIntrinsics,
        int registerBase,
        int? registerCount = null,
        int? returnValueSlot = null)
    {
        _configuration = configuration;
        _deviceIntrinsics = deviceIntrinsics;
        _returnValueSlot = returnValueSlot;

        var livenessAnalysis = LivenessAnalysis.Analyse(controlFlowGraph);
        var livenessRangeAnalysis = LivenessRangeAnalysis.Analyse(controlFlowGraph);
        var livenessRangeInterferenceAnalysis = LivenessRangeInterferenceAnalysis.Analyse(controlFlowGraph);
        var coloring = livenessRangeInterferenceAnalysis.LivenessRangeInterferencesGraph.GreedyColoring();
        _blockOrderAnalysis = BasicBlockOrderAnalysis.Analyse(controlFlowGraph, new NaiveCostModel());

        _registerMap = controlFlowGraph.GetVariables()
                                       .ToDictionary(
                                           variable => variable,
                                           variable => coloring[livenessRangeAnalysis.Ranges.Find(variable)] + registerBase);

        if (registerCount.HasValue && _registerMap.Values.Count > 0 && _registerMap.Values.Max() >= registerBase + registerCount.Value)
        {
            throw new NotSupportedException(
                $"This unit needs more registers ({_registerMap.Values.Max() - registerBase + 1}) than its allotted range ({registerCount} starting at {registerBase}) provides.");
        }

        _sequenceNumberMap = _blockOrderAnalysis.BlockOrder.Index()
                                                .ToDictionary(kvp => kvp.Item, kvp => 10 * (kvp.Index + 1));
        _blockJumpTargetMap = _blockOrderAnalysis.BlockOrder.ToDictionary(block => block.EntryOffset, block => block);
    }

    public string GenerateCode(params object[] arguments)
    {
        // NOTE: this "arguments" is the caller's raw Cnc.IDevice.Dispatch(...) arguments (arbitrary
        // CLR values of whatever type the dispatched method declares, e.g. int/double/string) — it
        // is unrelated to the TAC operand type system (OperandBase) and is never passed through
        // RenderOperand; it is only ever ToString()'d below. object[] is the correct, unavoidable
        // type here (there is no common supertype for "whatever the dispatched method's parameters
        // are" short of making this method generic over T1..T3, which would ripple through
        // Cnc.IDevice/StringDevice's whole public surface for no benefit to this feature).
        var sb = new StringBuilder();

        for (var argumentIndex = 0; argumentIndex < arguments.Length; ++argumentIndex)
        {
            var argumentVariable = new SsaVariable($"arg_{argumentIndex}");
            sb.AppendLine($"{RenderOperand(argumentVariable)} = {arguments[argumentIndex].ToString()}");
        }

        sb.Append(GenerateBlocksCode());

        return sb.ToString();
    }

    public string GenerateProcedureCode(int programNumber, int parameterCount, MacroCallConvention convention)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"O{programNumber}");

        for (var i = 0; i < parameterCount; ++i)
        {
            var argumentVariable = new SsaVariable($"arg_{i}");
            var sourceVariable = convention switch
            {
                MacroCallConvention.SubProgramCall => _configuration.ArgumentBaseVariable + i,
                MacroCallConvention.MacroCallStyle1 => MacroCallStyle1Arguments.Positions[i].LocalVariable,
                _ => throw new NotSupportedException(
                    $"GenerateProcedureCode does not support convention {convention}.")
            };
            sb.AppendLine($"{RenderOperand(argumentVariable)} = #{sourceVariable}");
        }

        sb.Append(GenerateBlocksCode());
        return sb.ToString();
    }

    private string GenerateBlocksCode()
    {
        var sb = new StringBuilder();

        for (var i = 0; i < _blockOrderAnalysis.BlockOrder.Count; ++i)
        {
            var block = _blockOrderAnalysis.BlockOrder[i];
            var successorJumpTarget = i + 1 == _blockOrderAnalysis.BlockOrder.Count
                                          ? null
                                          : new JumpTarget(_blockOrderAnalysis.BlockOrder[i + 1].EntryOffset);

            sb.Append(GenerateCode(block, successorJumpTarget));
        }

        return sb.ToString();
    }

    private string GenerateCode(TacInstructionBlock block, JumpTarget? successorJumpTarget)
    {
        var sb = new StringBuilder();
        if (block.Predecessors.Any())
        {
            sb.Append($"N{_sequenceNumberMap[block]} ");
        }

        foreach (var instruction in block.BodyInstructions)
        {
        if (instruction.Op == Operand.Call && instruction.Arguments.FirstOrDefault() is ProcedureCall)
        {
            sb.Append(RenderProcedureCall(instruction));
            continue;
        }

            if (instruction.Destination is SsaVariable destination)
            {
                sb.Append($"#{_registerMap[destination]} = ");
            }

            switch (instruction.Op)
            {
                case Operand.Assign:
                    sb.AppendLine($"{RenderOperand(instruction.Arguments.Single())}");
                    break;

                case Operand.Add:
                case Operand.Sub:
                case Operand.Mul:
                case Operand.Div:
                case Operand.Cgt:
                case Operand.Clt:
                case Operand.Ceq:
                case Operand.Or:
                case Operand.XOr:
                case Operand.And:
                    sb.AppendLine(
                        RenderInfixExpression(instruction.Op, instruction.Arguments.First(), instruction.Arguments.Last()));
                    break;

                case Operand.Pow:
                    sb.AppendLine(RenderPowExpression(instruction.Arguments.First(), instruction.Arguments.Last()));
                    break;
            case Operand.ATan2:
                sb.AppendLine(RenderAtan2Expression(instruction.Arguments.First(), instruction.Arguments.Last()));
                break;


                case Operand.Sin:
                case Operand.Cos:
                case Operand.Tan:
                case Operand.ASin:
                case Operand.ACos:
                case Operand.ATan:
                case Operand.Sqrt:
                case Operand.Abs:
                case Operand.Bin:
                case Operand.Bcd:
                case Operand.Round:
                case Operand.Fix:
                case Operand.Fup:
                case Operand.Ln:
                case Operand.Exp:
                case Operand.Adp:
                case Operand.Rem:
                case Operand.Neg:
                    sb.AppendLine(RenderPrefixExpression(instruction.Op, instruction.Arguments.First()));
                    break;

                case Operand.Call:
                    sb.AppendLine(RenderNcStatement(instruction));
                    break;

                default:
                    throw new NotSupportedException($"{instruction.Op}");
            }
        }

        if (block.BranchInstruction is TacInstruction branchInstruction)
        {
            switch (branchInstruction.Op)
            {
                case Operand.BrTrue:
                {
                    var condition = branchInstruction.Arguments.ElementAt(0);
                    var trueJumpTarget = branchInstruction.Arguments.ElementAt(1) as JumpTarget;
                    var falseJumpTarget = branchInstruction.Arguments.ElementAt(2) as JumpTarget;

                    if (successorJumpTarget is JumpTarget successor
                     && (successor == trueJumpTarget || successor == falseJumpTarget))
                    {
                        if (trueJumpTarget == successor)
                        {
                            sb.AppendLine(
                                $"IF[{RenderOperand(condition)} EQ 0]GOTO {RenderJumpTarget(falseJumpTarget)}");
                        }
                        else if (falseJumpTarget == successor)
                        {
                            sb.AppendLine($"IF[{RenderOperand(condition)}]GOTO {RenderJumpTarget(trueJumpTarget)}");
                        }
                    }
                    else
                    {
                        sb.AppendLine($"IF[{RenderOperand(condition)}]GOTO {RenderJumpTarget(trueJumpTarget)}");
                        sb.AppendLine($"GOTO {RenderJumpTarget(falseJumpTarget)}");
                    }
                }
                    break;
                case Operand.BrFalse:
                {
                    var condition = branchInstruction.Arguments.ElementAt(0);
                    var trueJumpTarget = branchInstruction.Arguments.ElementAt(1) as JumpTarget;
                    var falseJumpTarget = branchInstruction.Arguments.ElementAt(2) as JumpTarget;

                    if (successorJumpTarget is JumpTarget successor
                     && (successor == trueJumpTarget || successor == falseJumpTarget))
                    {
                        if (trueJumpTarget == successor)
                        {
                            sb.AppendLine($"IF[{RenderOperand(condition)}]GOTO {RenderJumpTarget(falseJumpTarget)}");
                        }
                        else if (falseJumpTarget == successor)
                        {
                            sb.AppendLine(
                                $"IF[{RenderOperand(condition)} EQ 0]GOTO {RenderJumpTarget(trueJumpTarget)}");
                        }
                    }
                    else
                    {
                        sb.AppendLine($"IF[{RenderOperand(condition)} EQ 0]GOTO {RenderJumpTarget(trueJumpTarget)}");
                        sb.AppendLine($"GOTO {RenderJumpTarget(falseJumpTarget)}");
                    }
                }
                    break;

                case Operand.Ble:
                case Operand.Blt:
                case Operand.Bge:
                case Operand.Bgt:
                case Operand.Beq:
                case Operand.Bne:
                {
                    var operand1 = branchInstruction.Arguments.ElementAt(0);
                    var operand2 = branchInstruction.Arguments.ElementAt(1);
                    var trueJumpTarget = branchInstruction.Arguments.ElementAt(2) as JumpTarget;
                    var falseJumpTarget = branchInstruction.Arguments.ElementAt(3) as JumpTarget;

                    // Fixed (see fix #1 above): the "no successor match" else-branch below is the
                    // unambiguous ground truth ("operand1 OP operand2 -> trueTarget else falseTarget").
                    // Eliding the jump to trueTarget (it's the fallthrough) requires jumping to
                    // falseTarget exactly when the condition is false, i.e. the INVERTED operator.
                    // Eliding the jump to falseTarget requires jumping to trueTarget exactly when the
                    // condition is true, i.e. the ORIGINAL operator — the previous code had both of
                    // these backwards, and additionally rendered operand2 on both sides in the second
                    // case instead of operand1/operand2.
                    if (successorJumpTarget is JumpTarget successor
                     && (successor == trueJumpTarget || successor == falseJumpTarget))
                    {
                        if (trueJumpTarget == successor)
                        {
                            sb.AppendLine(
                                $"IF[{RenderOperand(operand1)} {_binaryOperatorSymbols[Inverse(branchInstruction.Op)]} {RenderOperand(operand2)}]GOTO {RenderJumpTarget(falseJumpTarget)}");
                        }
                        else if (falseJumpTarget == successor)
                        {
                            sb.AppendLine(
                                $"IF[{RenderOperand(operand1)} {_binaryOperatorSymbols[branchInstruction.Op]} {RenderOperand(operand2)}]GOTO {RenderJumpTarget(trueJumpTarget)}");
                        }
                    }
                    else
                    {
                        sb.AppendLine(
                            $"IF[{RenderOperand(operand1)} {_binaryOperatorSymbols[branchInstruction.Op]} {RenderOperand(operand2)}]GOTO {RenderJumpTarget(trueJumpTarget)}");
                        sb.AppendLine($"GOTO {RenderJumpTarget(falseJumpTarget)}");
                    }
                }

                    break;

                case Operand.Br:
                {
                    var jumpTarget = branchInstruction.Arguments.ElementAt(0) as JumpTarget;
                    if (!(successorJumpTarget is JumpTarget successor
                     && successor == jumpTarget))
                    {
                        sb.AppendLine($"GOTO {RenderJumpTarget(branchInstruction.Arguments.Single())}");
                    }
                }
                    break;

                case Operand.Ret:
                    if (_returnValueSlot is int slot && branchInstruction.Arguments.Count > 0)
                    {
                        sb.AppendLine($"#{slot} = {RenderOperand(branchInstruction.Arguments.Single())}");
                    }
                    sb.AppendLine("M99");
                    break;

                default:
                    throw new NotSupportedException();
            }
        }

        return sb.ToString();
    }

    private static Operand Inverse(Operand op)
    {
        return op switch
        {
            Operand.BrTrue => Operand.BrFalse,
            Operand.BrFalse => Operand.BrTrue,
            Operand.Ble => Operand.Bgt,
            Operand.Blt => Operand.Bge,
            Operand.Bge => Operand.Blt,
            Operand.Bgt => Operand.Ble,
            Operand.Beq => Operand.Bne,
            Operand.Bne => Operand.Beq,
            _ => throw new NotSupportedException()
        };
    }

    // Was "private string RenderOperand(object operand)"; the parameter narrows to OperandBase now
    // that TacInstruction.Arguments is List<OperandBase> (step 1), so every call site below already
    // passes an OperandBase without needing a cast.
    private string RenderOperand(OperandBase operand)
    {
        return operand switch
        {
            Constant constant => constant.Value,
            SsaVariable variable => $"#{_registerMap[variable]}",
            CompositeUnaryExpression unary => RenderPrefixExpression(unary.Operand, unary.Expression),
            CompositeBinaryExpression binary => RenderInfixExpression(binary.Operand, binary.Left, binary.Right)
        };
    }

    private string RenderPrefixExpression(Operand op, OperandBase argument)
    {
        return $"{_functionPrefixNames[op]}[{RenderOperand(argument)}]";
    }

    // Fixed (see fix #3 above): Pow needs both operands (base, exponent); the previous per-case body
    // only rendered the first argument, silently discarding the exponent.
    private string RenderPowExpression(OperandBase baseValue, OperandBase exponent)
    {
        return $"POW[{RenderOperand(baseValue)},{RenderOperand(exponent)}]";
    }

    private string RenderAtan2Expression(OperandBase numerator, OperandBase denominator)
    {
        return $"ATAN[{RenderOperand(numerator)}]/[{RenderOperand(denominator)}]";
    }

    private string RenderProcedureCall(TacInstruction instruction)
    {
        var procedureCall = (ProcedureCall)instruction.Arguments[0];
        var callArguments = instruction.Arguments.Skip(1).ToList();
        var sb = new StringBuilder();

        switch (procedureCall.Convention)
        {
            case MacroCallConvention.SubProgramCall:
                for (var i = 0; i < callArguments.Count; ++i)
                {
                    sb.AppendLine($"#{_configuration.ArgumentBaseVariable + i} = {RenderOperand(callArguments[i])}");
                }
                sb.AppendLine($"M98 P{procedureCall.ProgramNumber}");
                break;

            case MacroCallConvention.MacroCallStyle1:
                var letterArgs = callArguments
                    .Select((arg, i) => $"{MacroCallStyle1Arguments.Positions[i].Letter}{RenderOperand(arg)}");
                sb.AppendLine($"G65 P{procedureCall.ProgramNumber} {string.Join(" ", letterArgs)}".TrimEnd());
                break;

            default:
                throw new NotSupportedException(
                    $"ProcedureCall convention {procedureCall.Convention} is not supported by code generation.");
        }

        if (instruction.Destination is SsaVariable destination)
        {
            sb.AppendLine($"#{_registerMap[destination]} = #{_configuration.ReturnValueVariable}");
        }

        return sb.ToString();
    }

    private string RenderInfixExpression(Operand op, OperandBase left, OperandBase right)
    {
        return $"{RenderChild(left, op, isRightOperand: false)}{_binaryOperatorSymbols[op]}{RenderChild(right, op, isRightOperand: true)}";
    }

    private string RenderChild(OperandBase child, Operand parentOperand, bool isRightOperand)
    {
        var text = RenderOperand(child);

        if (child is CompositeBinaryExpression childExpression
         && NeedsBrackets(childExpression.Operand, parentOperand, isRightOperand))
        {
            return $"[{text}]";
        }

        return text;
    }

    private static bool NeedsBrackets(Operand childOperand, Operand parentOperand, bool isRightOperand)
    {
        var childPrecedence = _binaryOperatorPrecedence[childOperand];
        var parentPrecedence = _binaryOperatorPrecedence[parentOperand];

        if (childPrecedence != parentPrecedence)
        {
            return childPrecedence < parentPrecedence;
        }

        return isRightOperand && _nonAssociativeBinaryOperators.Contains(parentOperand);
    }

    private string RenderNcStatement(TacInstruction callInstruction)
    {
        Debug.Assert(callInstruction.Op == Operand.Call);

        if (callInstruction.Arguments.First() is IntrinsicFunctionCall intrinsicFunctionCall)
        {
            return _deviceIntrinsics[intrinsicFunctionCall.FunctionName]
               .Render(callInstruction.Arguments.Skip(1).Select(RenderNcArgument));
        }

        throw new InvalidOperationException();
    }

    // FANUC word syntax: a letter address (X/Y/Z/F/...) takes a bare register (X#123) or bare
    // constant (X34) directly, but a composite expression or function-call result must be
    // parenthesized: X[SIN[...]], X[1+2]. CompositeExpression is the common base of both
    // CompositeUnaryExpression (function calls: SIN[...], SQRT[...], ...) and
    // CompositeBinaryExpression (arithmetic: 1+2, ...), so a single type check covers both.
    private string RenderNcArgument(OperandBase operand)
    {
        var text = RenderOperand(operand);
        return operand is CompositeExpression ? $"[{text}]" : text;
    }

    private string RenderJumpTarget(object argument)
    {
        if (argument is JumpTarget jumpTarget)
        {
            return _sequenceNumberMap[_blockJumpTargetMap[jumpTarget.Target]].ToString();
        }

        throw new NotImplementedException();
    }
}

// Data-only description of the ISO block (G/M-code plus positional word-address letters, e.g.
// X/Y/Z/F for a Move) a device intrinsic renders to. Owned and built by the front-end
// (MacroB.Host.FrontEnd.CncDeviceIntrinsics.Map), mirroring how CncMathIntrinsics.Map owns the
// Cnc.Math-method-to-Operand mapping; the code generator only knows how to turn a template plus
// already-rendered argument text into Macro B text, it never hardcodes which intrinsic maps to
// which G/M-code.
public sealed record NcStatementTemplate(string Code, IReadOnlyList<string> ArgumentLetters)
{
    public string Render(IEnumerable<string> arguments)
    {
        if (ArgumentLetters.Count == 0)
        {
            return Code;
        }

        return $"{Code} {string.Join(" ", ArgumentLetters.Zip(arguments, (letter, argument) => letter + argument))}";
    }
}
