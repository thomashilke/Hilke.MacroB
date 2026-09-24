using System.Diagnostics;

namespace Hilke.MacroB.FrontEnd;

[DebuggerDisplay("Block_{Id}")]
public class TacInstructionBlock : IAdjacencyVertex<TacInstructionBlock>
{
    private readonly List<TacInstructionBlock> _successors = new();
    private readonly List<TacInstructionBlock> _predecessors = new();

    public TacInstructionBlock(
        int id,
        int entryOffset,
        bool isEntry,
        IEnumerable<TacInstruction> bodyInstructions,
        TacInstruction? branchInstruction)
    {
        Id = id;
        EntryOffset = entryOffset;
        IsEntry = isEntry;

        BodyInstructions = bodyInstructions?.ToList() ?? throw new ArgumentNullException(nameof(bodyInstructions));
        BranchInstruction = branchInstruction;

        if (BodyInstructions.Count > 0 && BodyInstructions[^1].Op.IsControlFlow())
        {
            throw new ArgumentException("The last instruction should not be control flow");
        }

        if (branchInstruction is TacInstruction instr && !instr.Op.IsControlFlow())
        {
            throw new ArgumentException("The branch instruction should be control flow");
        }
    }

    public int Id { get; }

    public int EntryOffset { get; }

    public bool IsEntry { get; }

    public List<TacInstruction> Phis { get; } = new();

    public IReadOnlyList<TacInstruction> BodyInstructions { get; }

    public TacInstruction? BranchInstruction { get; }

    public IEnumerable<TacInstruction> Instructions =>
        Phis.Concat(
            BranchInstruction is null
                ? BodyInstructions
                : BodyInstructions.Append(BranchInstruction));

    public IReadOnlyList<TacInstructionBlock> Successors => _successors;

    public IReadOnlyList<TacInstructionBlock> Predecessors => _predecessors;

    public void RemoveInstruction(TacInstruction instruction)
    {
        if (!((List<TacInstruction>)BodyInstructions).Remove(instruction) && !Phis.Remove(instruction))
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

    public void ReplaceSuccessor(TacInstructionBlock currentSuccessor, TacInstructionBlock newSuccessor)
    {
        if (_successors.Remove(currentSuccessor))
        {
            throw new ArgumentException();
        }
        _successors.Add(newSuccessor);
    }

    public void ReplacePredecessor(TacInstructionBlock currentPredecessor, TacInstructionBlock newPredecessor)
    {
        if (_predecessors.Remove(currentPredecessor))
        {
            throw new ArgumentException();
        }
        _predecessors.Add(newPredecessor);
    }
}
