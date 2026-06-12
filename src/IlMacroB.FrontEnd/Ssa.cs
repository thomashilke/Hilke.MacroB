namespace Rollomatic.IlMacroB.FrontEnd;

public class SsaVariable
{
    public string Name { get; set; }
    public int Version { get; set; }
    public override string ToString() => $"{Name}_{Version}";
}

public class SsaInstruction
{
    public SsaVariable? Destination { get; set; }

    public string Op { get; set; }

    public List<object> Arguments { get; set; } = new();

    public override string ToString()
    {
        if (Op == "phi")
        {
            var args = string.Join(", ", Arguments);
            return $"{Destination} = phi({args})";
        }

        if (Destination is null)
        {
            return $"{Op} {string.Join(", ", Arguments)}".Trim();
        }

        var opString = Op == "assign" ? "" : $" {Op}";
        var argumentString = Arguments.Count > 1
            ? $"{Arguments[0]}{opString} {Arguments[1]}"
            : $"{Arguments.FirstOrDefault()}";

        return $"{Destination} = {argumentString.Trim()}";
    }
}


public class SsaBasicBlock
{
    public int Id { get; set; }
    public List<SsaInstruction> Phis { get; set; } = new();
    public List<SsaInstruction> Instructions { get; set; } = new();
    public List<SsaBasicBlock> Predecessors { get; set; } = new();
    public List<SsaBasicBlock> Successors { get; set; } = new();
}

public class SsaRenamer
{
    private Dictionary<string, int> _counter = new();
    private Dictionary<string, Stack<int>> _stack = new();

    public SsaRenamer(SsaBasicBlock entry, Dictionary<SsaBasicBlock, List<SsaBasicBlock>> dominatorTree)
    {
        var allVariables = entry.Instructions
            .Select(instruction => instruction.Destination?.Name)
            .Where(name => name is not null)
            .Distinct();

        foreach (var variable in allVariables)
        {
            _counter[variable] = 0;
            _stack[variable] = new Stack<int>();
            _stack[variable].Push(0);
        }

        RenameBlock(entry, dominatorTree);
    }

    private void RenameBlock(SsaBasicBlock block, Dictionary<SsaBasicBlock, List<SsaBasicBlock>> dominatorTree)
    {
        foreach (var phi in block.Phis)
        {
            phi.Destination.Version = NextVersion(phi.Destination.Name);
        }

        foreach (var instruction in block.Instructions)
        {
            for (var i = 0; i < instruction.Arguments.Count; ++i)
            {
                if (instruction.Arguments[i] is SsaVariable variable)
                {
                    variable.Version = CurrentVersion(variable.Name);
                }
            }

            if (instruction.Destination is not null)
            {
                instruction.Destination.Version = NextVersion(instruction.Destination.Name);
            }
        }

        foreach (var successor in block.Successors)
        {
            var predecessorIndex = successor.Predecessors.IndexOf(block);

            foreach (var phi in successor.Phis)
            {
                if (phi.Arguments[predecessorIndex] is SsaVariable variable)
                {
                    variable.Version = CurrentVersion(variable.Name);
                }
            }
        }

        if (dominatorTree.TryGetValue(block, out var children))
        {
            foreach (var child in children)
            {
                RenameBlock(child, dominatorTree);
            }
        }

        foreach (var phi in block.Phis)
        {
            _stack[phi.Destination.Name].Pop();
        }

        foreach (var instruction in block.Instructions.Where(instruction => instruction.Destination is not null))
        {
            _stack[instruction.Destination.Name].Pop();
        }
    }

    private int CurrentVersion(string name) => _stack.ContainsKey(name) && _stack[name].Count > 0 ? _stack[name].Peek() : 0;

    private int NextVersion(string name)
    {
        if (!_counter.ContainsKey(name))
        {
            _counter[name] = 0;
            _stack[name] = new();
        }

        var version = _counter[name]++;
        _stack[name].Push(version);
        return version;
    }
}

public class DominanceEngine
{
    public static void InsertPhiNodes(List<SsaBasicBlock> blocks, Dictionary<SsaBasicBlock, HashSet<SsaBasicBlock>> dominanceFrontier)
    {
        var variableDefinitions = new Dictionary<string, HashSet<SsaBasicBlock>>();

        foreach (var block in blocks)
        {
            foreach (var instruction in block.Instructions.Where(instruction => instruction.Destination is not null))
            {
                if (!variableDefinitions.ContainsKey(instruction.Destination.Name))
                {
                    variableDefinitions[instruction.Destination.Name] = new HashSet<SsaBasicBlock>();
                }

                variableDefinitions[instruction.Destination.Name].Add(block);
            }
        }

        foreach (var kvp in variableDefinitions)
        {
            var variableName = kvp.Key;
            var w = new Queue<SsaBasicBlock>(kvp.Value);
            var addedPhi = new HashSet<SsaBasicBlock>();

            while (w.Count > 0)
            {
                var n = w.Dequeue();
                foreach (var y in dominanceFrontier[n])
                {
                    if (!addedPhi.Contains(y))
                    {
                        var phi = new SsaInstruction
                        {
                            Destination = new SsaVariable { Name = variableName },
                            Op = "phi",
                        };

                        foreach (var predecessor in y.Predecessors)
                        {
                            phi.Arguments.Add(new SsaVariable { Name = variableName });
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

    public static Dictionary<SsaBasicBlock, HashSet<SsaBasicBlock>> ComputeFrontiers(
        List<SsaBasicBlock> blocks, SsaBasicBlock entry)
    {
        var dominances = ComputeDominators(blocks, entry);
        var frontiers = blocks.ToDictionary(b => b, b => new HashSet<SsaBasicBlock>());

        foreach (var block in blocks)
        {
            if (block.Predecessors.Count >= 2)
            {
                foreach (var predecessor in block.Predecessors)
                {
                    var runner = predecessor;
                    while (runner != dominances[block])
                    {
                        frontiers[runner].Add(block);
                        runner = dominances[runner];
                    }
                }
            }
        }

        return frontiers;
    }

    private static Dictionary<SsaBasicBlock, SsaBasicBlock> ComputeDominators(List<SsaBasicBlock> blocks, SsaBasicBlock entry)
    {
        var dominances = blocks.ToDictionary(block => block, _ => blocks.ToHashSet());

        dominances[entry] = new HashSet<SsaBasicBlock> { entry };

        bool changed = true;
        while (changed)
        {
            changed = false;

            foreach (var block in blocks)
            {
                var newDominance = block.Predecessors.Count == 0
                    ? new HashSet<SsaBasicBlock> { block }
                    : block.Predecessors.First() is not null ? dominances[block.Predecessors.First()].ToHashSet() : new HashSet<SsaBasicBlock>();

                for (var i = 1; i < block.Predecessors.Count; ++i)
                {
                    newDominance.IntersectWith(dominances[block.Predecessors[i]]);
                }

                newDominance.Add(block);

                if (!dominances[block].SetEquals(newDominance))
                {
                    dominances[block] = newDominance;
                    changed = true;
                }
            }
        }

        var immediateDominators = new Dictionary<SsaBasicBlock, SsaBasicBlock>();
        foreach (var block in blocks)
        {
            if (block == entry)
            {
                immediateDominators[entry] = entry;
                continue;
            }

            immediateDominators[block] = dominances[block].First(
                d =>
                d != block &&
                dominances[block].All(
                    other =>
                    other == block ||
                    other == d ||
                    dominances[other].Contains(d)));
        }

        return immediateDominators;
    }

}
