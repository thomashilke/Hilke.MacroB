using System.Runtime.ConstrainedExecution;
using System.Runtime.Intrinsics.Arm;

using static System.Net.WebRequestMethods;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Rollomatic.IlMacroB.FrontEnd;

public class ConstantPropagatorTransform : IControlFlowGraphTransformation<TacInstructionBlock>
{
    public ControlFlowGraph<TacInstructionBlock> Transform(ControlFlowGraph<TacInstructionBlock> controlFlowGraph)
    {
        var variables = controlFlowGraph.GetVariables();
        var variableUses = variables.ToDictionary(variable => variable, variable => new VariableDefinitionUses());
        var instructions = new List<TacInstruction>();
        foreach (var block in controlFlowGraph.Blocks)
        {
            foreach (var instruction in block.Instructions)
            {
                instructions.Add(instruction); ;
                foreach (var vars in instruction.Arguments
                    .Index()
                    .Where(p => p.Item is SsaVariable)
                    .Select(p => (p.Index, p.Item as SsaVariable)))
                {
                    variableUses[vars.Item2].AddUse(vars.Index, instruction);
                }
            }
        }

        var worklist = new Stack<TacInstruction>(instructions.Where(instruction => instruction.Arguments.All(argument => argument is Constant)));
        var substitutions = new Dictionary<SsaVariable, Constant>();

        while (worklist.Count > 0)
        {
            var instruction = worklist.Pop();
            if (instruction.Destination is { } destination)
            {
                Constant substitution = Evaluate(instruction, substitutions);
                substitutions[destination] = substitution;
                foreach (var use in variableUses[destination].Uses)
                {

                    use.Instruction.Arguments[use.ArgumentIndex] = substitution;
                    if (use.Instruction.Arguments.All(argument => argument is Constant))
                    {
                        worklist.Push(use.Instruction);
                    }
                }
            }
        }

        return controlFlowGraph;
    }

    private Constant Evaluate(TacInstruction instruction, Dictionary<SsaVariable, Constant> substitutions)
    {
        var constantArguments = instruction.Arguments.Select(argument =>
        {
            return argument switch
            {
                SsaVariable variable => substitutions[variable],
                Constant constant => constant
            };
        }).Select(c => double.Parse(c.Value)).ToList();

        var result = instruction.Op switch
        {
            Operand.Assign => constantArguments.First(),
            Operand.Sin => Math.Sin(constantArguments.First()),
            Operand.Cos => Math.Cos(constantArguments.First()),
            Operand.Tan => Math.Tan(constantArguments.First()),
            Operand.ASin => Math.Asin(constantArguments.First()),
            Operand.ACos => Math.Acos(constantArguments.First()),
            Operand.ATan => Math.Atan(constantArguments.First()),
            Operand.Sqrt => Math.Sqrt(constantArguments.First()),
            Operand.Abs => Math.Abs(constantArguments.First()),
            Operand.Bin => throw new NotSupportedException(),
            Operand.Bcd => throw new NotSupportedException(),
            Operand.Round => Math.Round(constantArguments.First()),
            Operand.Fix => Math.Ceiling(constantArguments.First()),
            Operand.Fup => Math.Floor(constantArguments.First()),
            Operand.Ln => Math.Log(constantArguments.First()),
            Operand.Exp => Math.Exp,
            Operand.Pow => Math.Pow(),
            Operand.Adp => ,
            Operand.Add => ,
            Operand.Sub => ,
            Operand.Mul => ,
            Operand.Div => ,
            Operand.Rem => ,
            Operand.Or => ,
            Operand.XOr => ,
            Operand.And => ,
            Operand.Not => ,
            Operand.Neg => ,
            Operand.Ceq => ,
            Operand.Clt => ,
            Operand.Cgt => ,
        }
    }
}