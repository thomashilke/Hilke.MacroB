namespace Rollomatic.IlMacroB.FrontEnd;

public static class ControlFlowGraphBuilder
{
    public static ControlFlowGraph BuildControlFlowGraph(List<IlInstruction> instructions)
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
            else if (instruction.OpCode.FlowControl == System.Reflection.Emit.FlowControl.Return ||
                     instruction.OpCode.FlowControl == System.Reflection.Emit.FlowControl.Throw)
            {
                leaders.Add(instruction.NextInstructionOffset);
            }
        }

        // Segment the instruction sequence into blocks
        var blocks = new List<BasicBlock>();
        BasicBlock? initialBlock = null;
        BasicBlock? currentBlock = null;
        var blockCounter = 0;

        foreach (var instruction in instructions)
        {
            if (leaders.Contains(instruction.Offset) || currentBlock is null)
            {
                var newBlock = new BasicBlock(id: blockCounter++);
                initialBlock = initialBlock is null ? newBlock : initialBlock;
                currentBlock = newBlock;
                blocks.Add(currentBlock);
            }

            currentBlock.Instructions.Add(instruction);
        }

        // Link the blocks
        var blockMap = blocks.ToDictionary(block => block.Instructions.First().Offset);
        foreach (var block in blocks)
        {
            var lastInstruction = block.Instructions.Last();

            if (lastInstruction.AbsoluteTarget.HasValue)
            {
                if (blockMap.TryGetValue(lastInstruction.AbsoluteTarget.Value, out var targetBlock))
                {
                    block.Successors.Add(targetBlock);
                }
            }

            if (lastInstruction.OpCode.FlowControl != System.Reflection.Emit.FlowControl.Branch &&
                lastInstruction.OpCode.FlowControl != System.Reflection.Emit.FlowControl.Return &&
                lastInstruction.OpCode.FlowControl != System.Reflection.Emit.FlowControl.Throw)
            {
                if (blockMap.TryGetValue(lastInstruction.NextInstructionOffset, out var nextBlock))
                {
                    block.Successors.Add(nextBlock);
                }
            }
        }

        foreach (var block in blocks)
        {
            foreach (var successor in block.Successors)
            {
                successor.Predecessors.Add(block);
            }
        }

        return new ControlFlowGraph(blocks, initialBlock);
    }
}
