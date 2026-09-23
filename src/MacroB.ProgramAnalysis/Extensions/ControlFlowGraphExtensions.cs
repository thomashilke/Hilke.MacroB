namespace Hilke.MacroB.FrontEnd;

public static class ControlFlowGraphExtensions
{
    extension(ControlFlowGraph<TacInstructionBlock> controlFlowGraph)
    {
        public IEnumerable<SsaVariable> GetVariables()
        {
            return controlFlowGraph.Blocks.SelectMany(GetBlockVariables).Distinct();

            static IEnumerable<SsaVariable> GetBlockVariables(TacInstructionBlock block)
            {
                return block.Instructions.SelectMany(GetInstructionVariables);
            }

            static IEnumerable<SsaVariable> GetInstructionVariables(TacInstruction instruction)
            {
                var destinationVariable = instruction.Destination is { } destination
                    ? new[] { destination }
                    : Enumerable.Empty<SsaVariable>();

                return destinationVariable.Concat(
                    instruction.Arguments.SelectMany(argument => argument.GetReferencedVariables()));
            }
        }

        public IEnumerable<TacInstruction> GetInstructions()
        {
            return controlFlowGraph.Blocks.SelectMany(block => block.Instructions);
        }
    }
}