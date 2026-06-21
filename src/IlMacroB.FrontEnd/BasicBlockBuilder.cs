using System.Reflection.Emit;

namespace Rollomatic.IlMacroB.FrontEnd;

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

    public IReadOnlyList<IlInstruction> Instructions => _instructions;

    public BasicBlockBuilder AddInstruction(IlInstruction instruction)
    {
        _instructions.Add(instruction);
        return this;
    }

    public BasicBlock Build()
    {
        var lastInstruction = _instructions.Last();
        var lastIsBranch =
            lastInstruction.OpCode.FlowControl == FlowControl.Branch
         || lastInstruction.OpCode.FlowControl == FlowControl.Return
         || lastInstruction.OpCode.FlowControl == FlowControl.Throw
         || lastInstruction.OpCode.FlowControl == FlowControl.Cond_Branch;

        if (lastIsBranch)
        {
            return new BasicBlock(_blockId, _isInitial, _instructions.SkipLast(1), _instructions.Last());
        }

        return new BasicBlock(_blockId, _isInitial, _instructions);
    }
}
