using System.Diagnostics;

namespace Rollomatic.IlMacroB.FrontEnd;

[DebuggerDisplay("Block_{Id}")]
public class TacInstructionBlock
{
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

        Successors = new();
        Predecessors = new();
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

    public List<TacInstructionBlock> Successors { get; set; }

    public List<TacInstructionBlock> Predecessors { get; set; }

    public void RemoveInstruction(TacInstruction usesDefinition)
    {
        if (!((List<TacInstruction>)BodyInstructions).Remove(usesDefinition) &&
            !Phis.Remove(usesDefinition))
        {
            //throw new InvalidOperationException("Cannot remove instruction: not found");
        }
    }
}