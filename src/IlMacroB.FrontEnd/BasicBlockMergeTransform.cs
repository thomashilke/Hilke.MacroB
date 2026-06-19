using System.Diagnostics;

namespace Rollomatic.IlMacroB.FrontEnd;

public class BasicBlockMergeTransform : IControlFlowGraphTransformation<TacInstructionBlock>
{
    /// <inheritdoc />
    public ControlFlowGraph<TacInstructionBlock> Transform(ControlFlowGraph<TacInstructionBlock> controlFlowGraph)
    {
        var paths = controlFlowGraph.Blocks.Where(block => block.Successors.Count() == 1)
                                    .Select(source => GetLinearPath(source).ToList())
                                    .Where(path => path.Count() > 1)
                                    .ToList();

        var replacedNodes = paths.SelectMany(x => x).ToHashSet();

        var mergedNodes = paths.Select(path => path.Aggregate(Merge)).ToList();
        var entryBlock = replacedNodes.Contains(controlFlowGraph.EntryBlock)
                             ? mergedNodes.ElementAt(
                                 paths.FindIndex(path => path.Contains(controlFlowGraph.EntryBlock)))
                             : controlFlowGraph.EntryBlock;

        var newCfg = new ControlFlowGraph<TacInstructionBlock>(
            controlFlowGraph.Blocks
                            .Except(replacedNodes)
                            .Concat(mergedNodes)
                            .ToList(),
            entryBlock);

        return newCfg;

        IEnumerable<TacInstructionBlock> GetLinearPath(TacInstructionBlock source)
        {
            while (source.Successors.Count() == 1 && source.Successors.Single().Predecessors.Count() == 1)
            {
                yield return source;
                source = source.Successors.Single();
            }
        }
    }

    private static TacInstructionBlock Merge(TacInstructionBlock block1, TacInstructionBlock block2)
    {
        var newBlock = new TacInstructionBlock(block1.BasicBlock, block1.BodyInstructions.Concat(block2.BodyInstructions), block2.BranchInstruction);
        
        newBlock.Phis.AddRange(block1.Phis);

        Debug.Assert(block2.Phis.Count() == 0);

        newBlock.AddPredecessors(block1.Predecessors);
        newBlock.AddSuccessors(block2.Successors);

        foreach (var predecessor in block1.Predecessors)
        {
            predecessor.ReplaceSuccessor(block1, newBlock);
        }

        foreach (var successor in block2.Successors)
        {
            successor.ReplacePredecessor(block2, newBlock);
        }

        return newBlock;
    }
}