namespace Rollomatic.IlMacroB.FrontEnd;

public class BasicBlock
{
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

    public List<BasicBlock> Successors { get; } = new();

    public List<BasicBlock> Predecessors { get; } = new();
}