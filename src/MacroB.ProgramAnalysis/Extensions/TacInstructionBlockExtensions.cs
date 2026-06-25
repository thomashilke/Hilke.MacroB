namespace Hilke.MacroB.FrontEnd;

public static class TacInstructionBlockExtensions
{
    extension(TacInstructionBlock block)
    {
        public IEnumerable<SsaVariable> GetUniqueVariableDefinitions()
        {
            return block.Instructions.Select(instruction => instruction.Destination).OfType<SsaVariable>().Distinct();
        }

        public IEnumerable<SsaVariable> GetUniqueVariableUses()
        {
            return block.Instructions.SelectMany(instruction => instruction.Arguments.OfType<SsaVariable>()).Distinct();
        }
    }
}