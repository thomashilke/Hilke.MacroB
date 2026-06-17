using System.Diagnostics;

namespace Rollomatic.IlMacroB.FrontEnd;

[DebuggerDisplay("Block_{Id}")]
public class TacInstructionBlock : IAdjacencyVertex<TacInstructionBlock>
{
    private List<TacInstructionBlock> _successors;
    private List<TacInstructionBlock> _predecessors;

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

        _successors = new();
        _predecessors = new();
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

    public IReadOnlyList<TacInstructionBlock> Successors
    {
        get => _successors;
    }

    public IReadOnlyList<TacInstructionBlock> Predecessors
    {
        get => _predecessors;
    }

    public void RemoveInstruction(TacInstruction usesDefinition)
    {
        if (!((List<TacInstruction>)BodyInstructions).Remove(usesDefinition) &&
            !Phis.Remove(usesDefinition))
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