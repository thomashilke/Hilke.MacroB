using System.Diagnostics;

namespace Rollomatic.IlMacroB.FrontEnd;

[DebuggerDisplay("Block_{Id}")]
public class TacInstructionBlock : IAdjacencyVertex<TacInstructionBlock>
{
    private readonly List<TacInstructionBlock> _successors;
    private readonly List<TacInstructionBlock> _predecessors;

    public TacInstructionBlock(
        BasicBlock basicBlock,
        IEnumerable<TacInstruction> instructions,
        TacInstruction? branchInstruction,
        List<string> incomingStack,
        List<string> outgoingStack)
    {
        BasicBlock = basicBlock ?? throw new ArgumentNullException(nameof(basicBlock));
        EntryOffset = BasicBlock.Instructions.First().Offset;

        BodyInstructions = instructions?.ToList() ?? throw new ArgumentNullException(nameof(instructions));
        BranchInstruction = branchInstruction;

        IncomingStack = incomingStack ?? throw new ArgumentNullException(nameof(incomingStack));
        OutgoingStack = outgoingStack ?? throw new ArgumentNullException(nameof(outgoingStack));

        _successors = new List<TacInstructionBlock>();
        _predecessors = new List<TacInstructionBlock>();
    }

    public int Id => BasicBlock.Id;

    public int EntryOffset { get; }

    public bool IsEntry => BasicBlock.IsInitial;

    public BasicBlock BasicBlock { get; }

    public List<TacInstruction> Phis { get; } = new();

    public IReadOnlyList<TacInstruction> BodyInstructions { get; }

    public TacInstruction? BranchInstruction { get; }

    public IEnumerable<TacInstruction> Instructions =>
        Phis.Concat(
            BranchInstruction is null
                ? BodyInstructions
                : BodyInstructions.Append(BranchInstruction));

    public List<string> IncomingStack { get; }

    public List<string> OutgoingStack { get; }

    public IReadOnlyList<TacInstructionBlock> Successors => _successors;

    public IReadOnlyList<TacInstructionBlock> Predecessors => _predecessors;

    public void RemoveInstruction(TacInstruction usesDefinition)
    {
        if (!((List<TacInstruction>)BodyInstructions).Remove(usesDefinition) && !Phis.Remove(usesDefinition))
        {
            //throw new InvalidOperationException("Cannot remove instruction: not found");
        }
    }

    public void AddPredecessors(IEnumerable<TacInstructionBlock> predecessors)
    {
        _predecessors.AddRange(predecessors);
    }

    public void AddSuccessors(IEnumerable<TacInstructionBlock> successors)
    {
        _successors.AddRange(successors);
    }
}

// Or MacroBCodeGenerator?