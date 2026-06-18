using System.Reflection.Emit;

namespace Rollomatic.IlMacroB.FrontEnd;

public static class ControlFlowGraphBuilder
{
    public static ControlFlowGraph<BasicBlock> Create(List<IlInstruction> instructions)
    {
        if (!instructions.Any())
        {
            throw new InvalidOperationException();
        }

        // Identify the start offset of each blocks
        var leaders = new HashSet<int> { instructions[0].Offset };

        foreach (var instruction in instructions)
        {
            if (instruction.AbsoluteTarget.HasValue)
            {
                leaders.Add(instruction.AbsoluteTarget.Value);
                leaders.Add(instruction.NextInstructionOffset);
            }
            else if (instruction.OpCode.FlowControl == FlowControl.Return
                  || instruction.OpCode.FlowControl == FlowControl.Throw)
            {
                leaders.Add(instruction.NextInstructionOffset);
            }
        }

        // Segment the instruction sequence into blocks
        var blockBuilders = new List<BasicBlockBuilder>();
        BasicBlockBuilder? currentBlock = null;
        var blockCounter = 0;

        foreach (var instruction in instructions)
        {
            if (leaders.Contains(instruction.Offset) || currentBlock is null)
            {
                currentBlock = new BasicBlockBuilder(blockId: blockCounter++, isInitial: currentBlock is null);
                blockBuilders.Add(currentBlock);
            }

            currentBlock.AddInstruction(instruction);
        }

        var blocks = blockBuilders.Select(blockBuilder => blockBuilder.Build()).ToList();

        // Link the blocks
        var blockMap = blocks.ToDictionary(block => block.Instructions.First().Offset);
        foreach (var block in blocks)
        {
            var lastInstruction = block.BranchInstruction ?? block.Instructions.Last();

            if (lastInstruction.AbsoluteTarget.HasValue)
            {
                if (blockMap.TryGetValue(lastInstruction.AbsoluteTarget.Value, out var targetBlock))
                {
                    block.AddSuccessor(targetBlock);
                }
            }

            if (lastInstruction.OpCode.FlowControl != FlowControl.Branch
             && lastInstruction.OpCode.FlowControl != FlowControl.Return
             && lastInstruction.OpCode.FlowControl != FlowControl.Throw)
            {
                if (blockMap.TryGetValue(lastInstruction.NextInstructionOffset, out var nextBlock))
                {
                    block.AddSuccessor(nextBlock);
                }
            }
        }

        foreach (var block in blocks)
        {
            foreach (var successor in block.Successors)
            {
                successor.AddPredecessor(block);
            }
        }

        return new ControlFlowGraph<BasicBlock>(blocks, blocks.Single(block => block.IsInitial));
    }
}