namespace Rollomatic.IlMacroB.FrontEnd;

public class ControlFlowGraph : IGraph<BasicBlock>
{
    public ControlFlowGraph(IReadOnlyList<BasicBlock> blocks, BasicBlock initialBlock)
    {
        Blocks = blocks ?? throw new ArgumentNullException(nameof(blocks));
        InitialBlock = initialBlock ?? throw new ArgumentNullException(nameof(initialBlock));
    }

    public IReadOnlyList<BasicBlock> Blocks { get; }

    public BasicBlock InitialBlock { get; }

    IEnumerable<BasicBlock> IGraph<BasicBlock>.GetPredecessors(BasicBlock vertex) => vertex.Predecessors;
    IEnumerable<BasicBlock> IGraph<BasicBlock>.GetSuccessors(BasicBlock vertex) => vertex.Successors;
}

public class BasicBlockBuilder
{
    private readonly int _blockId;
    private readonly bool _isInitial;

    private readonly List<IlInstruction> _instructions = new();
    private readonly List<BasicBlockBuilder> _successors = new();
    private readonly List<BasicBlockBuilder> _predecessors = new();

    public BasicBlockBuilder(int blockId, bool isInitial = false)
    {
        _blockId = blockId;
        _isInitial = isInitial;
    }

    public IList<BasicBlockBuilder> Successors => _successors;

    public IList<BasicBlockBuilder> Predecessors => _predecessors;

    public BasicBlockBuilder AddInstruction(IlInstruction instruction)
    {
        _instructions.Add(instruction);
        return this;
    }

    public BasicBlock Build()
    {
        var lastInstruction = _instructions.Last();
        var lastIsBranch =
            lastInstruction.OpCode.FlowControl == System.Reflection.Emit.FlowControl.Branch ||
            lastInstruction.OpCode.FlowControl == System.Reflection.Emit.FlowControl.Return ||
            lastInstruction.OpCode.FlowControl == System.Reflection.Emit.FlowControl.Throw;

        if (lastIsBranch)
        {
            return new BasicBlock(_blockId, _isInitial, _instructions.SkipLast(1), _instructions.Last());
        }
        else
        {
            return new BasicBlock(_blockId, _isInitial, _instructions);
        }
    }
}

public class BasicBlock
{
    internal BasicBlock(int id, bool isInitial, IEnumerable<IlInstruction> instructions, IlInstruction? branchInstruction = null)
    {
        Id = id;
        IsInitial = isInitial;

        Instructions = instructions?.ToList() ?? throw new ArgumentNullException(nameof(instructions));
        BranchInstruction = branchInstruction;
    }

    public int Id { get; }

    public bool IsInitial { get; }

    public IReadOnlyList<IlInstruction> Instructions { get; }

    public IlInstruction? BranchInstruction { get; }

    public List<BasicBlock> Successors { get; } = new();

    public List<BasicBlock> Predecessors { get; } = new();
}
