namespace Hilke.MacroB.FrontEnd;

public class DominanceEngine
{
    public static void InsertPhiNodes(
        IReadOnlyList<TacInstructionBlock> blocks,
        Dictionary<TacInstructionBlock, IReadOnlyList<TacInstructionBlock>> dominanceFrontier)
    {
        var variableDefinitions = new Dictionary<string, HashSet<TacInstructionBlock>>();

        foreach (var block in blocks)
        {
            foreach (var instruction in block.Instructions.Where(instruction => instruction.Destination is not null))
            {
                if (!variableDefinitions.ContainsKey(instruction.Destination.Name))
                {
                    variableDefinitions[instruction.Destination.Name] = new HashSet<TacInstructionBlock>();
                }

                variableDefinitions[instruction.Destination.Name].Add(block);
            }
        }

        foreach (var kvp in variableDefinitions)
        {
            var variableName = kvp.Key;
            var w = new Queue<TacInstructionBlock>(kvp.Value);
            var addedPhi = new HashSet<TacInstructionBlock>();

            while (w.Count > 0)
            {
                var n = w.Dequeue();
                foreach (var y in dominanceFrontier[n])
                {
                    if (!addedPhi.Contains(y))
                    {
                        var phi = new TacInstruction(variableName, Operand.Phi);

                        foreach (var predecessor in y.Predecessors)
                        {
                            phi.Arguments.Add(new SsaVariable(variableName));
                        }

                        y.Phis.Add(phi);
                        addedPhi.Add(y);

                        if (!kvp.Value.Contains(y))
                        {
                            w.Enqueue(y);
                        }
                    }
                }
            }
        }
    }

    public static Dictionary<TacInstructionBlock, IReadOnlyList<TacInstructionBlock>> ComputeFrontiers(
        IReadOnlyList<TacInstructionBlock> blocks,
        TacInstructionBlock entry)
    {
        var immediateDominator = ComputeImmediateDominator(blocks, entry);
        var frontiers = blocks.ToDictionary(b => b, b => new HashSet<TacInstructionBlock>());

        foreach (var block in blocks)
        {
            if (block.Predecessors.Count() >= 2)
            {
                foreach (var predecessor in block.Predecessors)
                {
                    var runner = predecessor;
                    while (runner != immediateDominator[block])
                    {
                        frontiers[runner].Add(block);
                        runner = immediateDominator[runner];
                    }
                }
            }
        }

        return frontiers.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToList() as IReadOnlyList<TacInstructionBlock>);
    }

    public static DominatorTree ComputeDominatorTree(ControlFlowGraph<TacInstructionBlock> controlFlowGraph)
    {
        return new DominatorTree(ComputeImmediateDominator(controlFlowGraph.Blocks, controlFlowGraph.EntryBlock));
    }

    public static DominatorTree ComputeDominatorTree(IReadOnlyList<TacInstructionBlock> blocks)
    {
        return new DominatorTree(ComputeImmediateDominator(blocks, blocks.Single(block => block.IsEntry)));
    }

    public static Dictionary<TacInstructionBlock, TacInstructionBlock> ComputeImmediateDominator(
        IReadOnlyList<TacInstructionBlock> blocks,
        TacInstructionBlock entry)
    {
        var dominances = ComputeDominatorTree(blocks, entry);

        var immediateDominators = new Dictionary<TacInstructionBlock, TacInstructionBlock>();
        foreach (var block in blocks)
        {
            if (block == entry)
            {
                immediateDominators[entry] = entry;
                continue;
            }

            immediateDominators[block] = dominances[block]
                .Single(d =>
                            d != block
                         && // i.e. d strictly dominates block,
                            dominances[block]
                                .All(other =>
                                         other == block || other == d || !dominances[other].Contains(d)));
        }

        return immediateDominators;
    }

    /// <summary>
    ///     The Dominated By tree
    /// </summary>
    /// <param name="blocks"></param>
    /// <param name="entry"></param>
    /// <returns>A dictionary which associate the set of dominator block to each dominator. </returns>
    public static Dictionary<TacInstructionBlock, IReadOnlyList<TacInstructionBlock>> ComputeDominatorTree(
        IReadOnlyList<TacInstructionBlock> blocks,
        TacInstructionBlock entry)
    {
        var dominatorTree = blocks.ToDictionary(block => block, _ => blocks.ToHashSet());

        dominatorTree[entry] = new HashSet<TacInstructionBlock> { entry };

        var changed = true;
        while (changed)
        {
            changed = false;

            foreach (var block in blocks)
            {
                var newDominators = block.Predecessors.Count() == 0
                                        ? new HashSet<TacInstructionBlock> { block }
                                        : block.Predecessors.First() is not null
                                            ? dominatorTree[block.Predecessors.First()].ToHashSet()
                                            : new HashSet<TacInstructionBlock>();

                for (var i = 1; i < block.Predecessors.Count(); ++i)
                {
                    newDominators.IntersectWith(dominatorTree[block.Predecessors.ElementAt(i)]);
                }

                newDominators.Add(block);

                if (!dominatorTree[block].SetEquals(newDominators))
                {
                    dominatorTree[block] = newDominators;
                    changed = true;
                }
            }
        }

        return dominatorTree.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.ToList() as IReadOnlyList<TacInstructionBlock>);
    }
}