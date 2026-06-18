using System.Diagnostics;
using System.Text;

namespace Rollomatic.IlMacroB.FrontEnd;


public class MacroBCodeGenerator
{
    private readonly Dictionary<SsaVariable, int> _registerMap;
    private readonly Dictionary<TacInstructionBlock, int> _sequenceNumberMap;
    private readonly Dictionary<int, TacInstructionBlock> _blockJumpTargetMap;

    public MacroBCodeGenerator(ControlFlowGraph<TacInstructionBlock> controlFlowGraph)
    {
        var livenessAnalysis = LivenessAnalysis.Analyse(controlFlowGraph);
        var livenessRangeAnalysis = LivenessRangeAnalysis.Analyse(controlFlowGraph);
        var livenessRangeInterferenceAnalysis = LivenessRangeInterferenceAnalysis.Analyse(controlFlowGraph);
        var coloring = livenessRangeInterferenceAnalysis.LivenessRangeInterferencesGraph.GreedyColoring();
        var blockOrderAnalysis = BasicBlockOrderAnalysis.Analyse(controlFlowGraph, new NaiveCostModel());

        _registerMap = controlFlowGraph.GetVariables()
                                       .ToDictionary(
                                           variable => variable,
                                           variable => coloring[livenessRangeAnalysis.Ranges.Find(variable)]);

        _sequenceNumberMap = blockOrderAnalysis.BlockOrder.Index()
                                               .ToDictionary(kvp => kvp.Item, kvp => 10 * (kvp.Index + 1));
        _blockJumpTargetMap = blockOrderAnalysis.BlockOrder.ToDictionary(block => block.EntryOffset, block => block);
    }

    public string GenerateCode(TacInstructionBlock block)
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
                    sb.AppendLine(
                        $"IF[{RenderOperand(branchInstruction.Arguments.ElementAt(0))} NEQ 0]GOTO {RenderJumpTarget(branchInstruction.Arguments.ElementAt(1))}");
                    sb.AppendLine($"GOTO {RenderJumpTarget(branchInstruction.Arguments.ElementAt(2))}");
                    break;
                case Operand.BrFalse:
                    sb.AppendLine(
                        $"IF[{RenderOperand(branchInstruction.Arguments.ElementAt(0))} EQ 0]GOTO {RenderJumpTarget(branchInstruction.Arguments.ElementAt(1))}");
                    sb.AppendLine($"GOTO {RenderJumpTarget(branchInstruction.Arguments.ElementAt(2))}");
                    break;

                case Operand.Ble:
                case Operand.Blt:
                case Operand.Bge:
                case Operand.Bgt:
                case Operand.Beq:
                case Operand.Bne:
                    sb.AppendLine(
                        $"IF[{RenderOperand(branchInstruction.Arguments.ElementAt(0))} {binaryOperatorMap[branchInstruction.Op]} {RenderOperand(branchInstruction.Arguments.ElementAt(1))}]GOTO {RenderJumpTarget(branchInstruction.Arguments.ElementAt(2))}");
                    sb.AppendLine($"GOTO {RenderJumpTarget(branchInstruction.Arguments.ElementAt(3))}");
                    break;

                case Operand.Br:
                    sb.AppendLine($"GOTO {RenderJumpTarget(branchInstruction.Arguments.Single())}");
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

    private string RenderOperand(object operand)
    {
        return operand switch
        {
            Constant constant => constant.Value,
            SsaVariable variable => $"#{_registerMap[variable]}"
        };
    }

    private static string RenderNcStatement(TacInstruction callInstruction)
    {
        var functionNameMap = new Dictionary<string, string>
        {
            { "StartCoolant", "M35" },
            { "StopCoolant", "M39" }
        };

        Debug.Assert(callInstruction.Op == Operand.Call);

        if (callInstruction.Arguments.First() is FunctionCall functionCall)
        {
            return $"{functionNameMap[functionCall.FunctionName]}";
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