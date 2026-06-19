using System.Diagnostics;

namespace Rollomatic.IlMacroB.FrontEnd;

[DebuggerDisplay("Block_{Id}")]
public class TacInstructionBlock : IAdjacencyVertex<TacInstructionBlock>
{
    private readonly List<TacInstructionBlock> _successors;
    private readonly List<TacInstructionBlock> _predecessors;

    public TacInstructionBlock(
        BasicBlock basicBlock,
        IEnumerable<TacInstruction> bodyInstructions,
        TacInstruction? branchInstruction)
    {
        BasicBlock = basicBlock ?? throw new ArgumentNullException(nameof(basicBlock));
        EntryOffset = BasicBlock.Instructions.First().Offset;

        BodyInstructions = bodyInstructions?.ToList() ?? throw new ArgumentNullException(nameof(bodyInstructions));
        BranchInstruction = branchInstruction;

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