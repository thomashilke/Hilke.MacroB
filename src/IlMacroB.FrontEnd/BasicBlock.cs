namespace Rollomatic.IlMacroB.FrontEnd;

public class BasicBlock : IAdjacencyVertex<BasicBlock>
{
    private readonly List<BasicBlock> _successors = new();
    private readonly List<BasicBlock> _predecessors = new();

    internal BasicBlock(
        int id,
        bool isInitial,
        IEnumerable<IlInstruction> bodyInstructions,
        IlInstruction? branchInstruction = null)
    {
        Id = id;
        IsInitial = isInitial;

        BodyInstructions = bodyInstructions?.ToList() ?? throw new ArgumentNullException(nameof(bodyInstructions));
        BranchInstruction = branchInstruction;
    }

    public int Id { get; }

    public bool IsInitial { get; }

    public IEnumerable<IlInstruction> Instructions =>
        BranchInstruction is not null ? BodyInstructions.Append(BranchInstruction) : BodyInstructions;

    public IReadOnlyList<IlInstruction> BodyInstructions { get; }

    public IlInstruction? BranchInstruction { get; }

    public IReadOnlyList<BasicBlock> Successors => _successors;

    public IReadOnlyList<BasicBlock> Predecessors => _predecessors;

    public void AddSuccessor(BasicBlock targetBlock)
    {
        _successors.Add(targetBlock);
    }

    public void AddPredecessor(BasicBlock block)
    {
        _predecessors.Add(block);
    }
}