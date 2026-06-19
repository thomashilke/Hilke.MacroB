using System.Diagnostics;
using System.Text;

namespace Rollomatic.IlMacroB.FrontEnd;

public class MacroBCodeGenerator
{
    private readonly Dictionary<SsaVariable, int> _registerMap;
    private readonly Dictionary<TacInstructionBlock, int> _sequenceNumberMap;
    private readonly Dictionary<int, TacInstructionBlock> _blockJumpTargetMap;
    private readonly BasicBlockOrderAnalysis _blockOrderAnalysis;

    public MacroBCodeGenerator(ControlFlowGraph<TacInstructionBlock> controlFlowGraph)
    {
        var livenessAnalysis = LivenessAnalysis.Analyse(controlFlowGraph);
        var livenessRangeAnalysis = LivenessRangeAnalysis.Analyse(controlFlowGraph);
        var livenessRangeInterferenceAnalysis = LivenessRangeInterferenceAnalysis.Analyse(controlFlowGraph);
        var coloring = livenessRangeInterferenceAnalysis.LivenessRangeInterferencesGraph.GreedyColoring();
        _blockOrderAnalysis = BasicBlockOrderAnalysis.Analyse(controlFlowGraph, new NaiveCostModel());

        _registerMap = controlFlowGraph.GetVariables()
                                       .ToDictionary(
                                           variable => variable,
                                           variable => coloring[livenessRangeAnalysis.Ranges.Find(variable)] + 1);

        _sequenceNumberMap = _blockOrderAnalysis.BlockOrder.Index()
                                                .ToDictionary(kvp => kvp.Item, kvp => 10 * (kvp.Index + 1));
        _blockJumpTargetMap = _blockOrderAnalysis.BlockOrder.ToDictionary(block => block.EntryOffset, block => block);
    }

    public string GenerateCode(params object[] arguments)
    {
        var sb = new StringBuilder();

        for (var argumentIndex = 0; argumentIndex < arguments.Length; ++argumentIndex)
        {
            var argumentVariable = new SsaVariable($"arg_{argumentIndex}");
            sb.AppendLine($"{RenderOperand(argumentVariable)} = {arguments[argumentIndex].ToString()}");
        }

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
        var binaryOperatorMap = new Dictionary<Operand, string>
        {
            { Operand.Add, "+" },
            { Operand.Sub, "-" },
            { Operand.Mul, "*" },
            { Operand.Div, "/" },
            { Operand.Cgt, "<" },
            { Operand.Ble, "LE" },
            { Operand.Blt, "LT" },
            { Operand.Bge, "GE" },
            { Operand.Bgt, "GT" },
            { Operand.Beq, "EQ" },
            { Operand.Bne, "NE" }
        };

        var sb = new StringBuilder();
        if (block.Predecessors.Any())
        {
            sb.Append($"N{_sequenceNumberMap[block]} ");
        }

        foreach (var instruction in block.BodyInstructions)
        {
            if (instruction.Destination is SsaVariable destination)
            {
                sb.Append($"#{_registerMap[destination]} = ");
            }

            switch (instruction.Op)
            {
                case Operand.Assign:
                    sb.AppendLine($"{RenderOperand(instruction.Arguments.Single() as SsaVariable)}");
                    break;

                case Operand.Add:
                case Operand.Sub:
                case Operand.Mul:
                case Operand.Div:
                case Operand.Cgt:
                    sb.AppendLine(
                        $"{RenderOperand(instruction.Arguments.First())}{binaryOperatorMap[instruction.Op]}{RenderOperand(instruction.Arguments.Last())}");
                    break;

                case Operand.Sin:
                    sb.AppendLine($"SIN[{RenderOperand(instruction.Arguments.First())}]");
                    break;

                case Operand.Call:
                    sb.AppendLine(RenderNcStatement(instruction));
                    break;

                default:
                    throw new NotSupportedException();
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

                    if (successorJumpTarget is JumpTarget successor
                     && (successor == trueJumpTarget || successor == falseJumpTarget))
                    {
                        if (trueJumpTarget == successor)
                        {
                            sb.AppendLine(
                                $"IF[{RenderOperand(operand1)} {binaryOperatorMap[branchInstruction.Op]} {RenderOperand(operand2)}]GOTO {RenderJumpTarget(falseJumpTarget)}");
                        }
                        else if (falseJumpTarget == successor)
                        {
                            sb.AppendLine(
                                $"IF[{RenderOperand(operand2)} {binaryOperatorMap[Inverse(branchInstruction.Op)]} {RenderOperand(operand2)}]GOTO {RenderJumpTarget(trueJumpTarget)}");
                        }
                    }
                    else
                    {
                        sb.AppendLine(
                            $"IF[{RenderOperand(operand1)} {binaryOperatorMap[branchInstruction.Op]} {RenderOperand(operand2)}]GOTO {RenderJumpTarget(trueJumpTarget)}");
                        sb.AppendLine($"GOTO {RenderJumpTarget(falseJumpTarget)}");
                    }
                }

                    break;

                case Operand.Br:
                {
                    var jumpTarget = branchInstruction.Arguments.ElementAt(0);
                    if (successorJumpTarget is not JumpTarget successor
                     || successor == jumpTarget)
                    {
                        sb.AppendLine($"GOTO {RenderJumpTarget(branchInstruction.Arguments.Single())}");
                    }
                }
                    break;

                case Operand.Ret:
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

    private string RenderOperand(object operand)
    {
        return operand switch
        {
            Constant constant => constant.Value,
            SsaVariable variable => $"#{_registerMap[variable]}"
        };
    }

    private string RenderNcStatement(TacInstruction callInstruction)
    {
        var functionNameMap = new List<NcStatement>
        {
            new StartCoolantStatement(),
            new StopCoolantStatement(),
            new MoveStatement()
        }.ToDictionary(statement => statement.Name);

        Debug.Assert(callInstruction.Op == Operand.Call);

        if (callInstruction.Arguments.First() is FunctionCall functionCall)
        {
            return
                $"{functionNameMap[functionCall.FunctionName].Render(callInstruction.Arguments.Skip(1).Select(arg => RenderOperand(arg)))}";
        }

        throw new InvalidOperationException();
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

internal abstract class NcStatement
{
    private protected NcStatement(string name)
    {
        Name = name;
    }

    public string Name { get; }

    public abstract string Render(IEnumerable<object> arguments);
}

internal sealed class MoveStatement : NcStatement
{
    public MoveStatement()
        : base("Move") { }

    public string GCode => "G01";

    public override string Render(IEnumerable<object> arguments)
    {
        var parameters = new[] { "X", "Y", "Z", "F" };
        return $"{GCode} {string.Join(" ", parameters.Zip(arguments, (p, a) => p + a))}";
    }
}

internal sealed class StopCoolantStatement : NcStatement
{
    public StopCoolantStatement()
        : base("StopCoolant") { }

    public string MCode => "M39";

    public override string Render(IEnumerable<object> arguments)
    {
        return MCode;
    }
}

internal sealed class StartCoolantStatement : NcStatement
{
    public StartCoolantStatement()
        : base("StartCoolant") { }

    public string MCode => "M35";

    public override string Render(IEnumerable<object> arguments)
    {
        return MCode;
    }
}