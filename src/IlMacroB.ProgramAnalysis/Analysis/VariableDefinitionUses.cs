namespace Rollomatic.IlMacroB.FrontEnd;

public sealed class VariableDefinitionUses
{
    private readonly List<(int, TacInstruction)> _uses = new();

    public VariableDefinitionUses(TacInstruction definition, TacInstructionBlock block)
    {
        Definition = definition;
        Block = block;
    }

    public VariableDefinitionUses() { }

    public TacInstruction? Definition { get; set; }

    public TacInstructionBlock? Block { get; set; }

    public IReadOnlyList<(int ArgumentIndex, TacInstruction Instruction)> Uses => _uses;

    public void AddUse(int argumentIndex, TacInstruction instruction)
    {
        _uses.Add((argumentIndex, instruction));
    }
}