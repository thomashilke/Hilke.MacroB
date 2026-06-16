namespace Rollomatic.IlMacroB.FrontEnd;

public class SsaRecursiveRenamer
{
    private readonly Dictionary<string, int> _counter = new();
    private readonly Dictionary<string, Stack<int>> _stack = new();

    public SsaRecursiveRenamer(
        TacInstructionBlock entry,
        Dictionary<TacInstructionBlock, TacInstructionBlock> immediateDominator)
    {
        var allVariables = entry.Instructions
                                .Select(instruction => instruction.Destination?.Name)
                                .Where(name => name is not null)
                                .Distinct();

        foreach (var variable in allVariables)
        {
            _counter[variable] = 0;
            _stack[variable] = new Stack<int>();
            _stack[variable].Push(1);
        }

        var toto = new Dictionary<TacInstructionBlock, HashSet<TacInstructionBlock>>();
        foreach (var (dominated, dominator) in immediateDominator)
        {
            if (dominated == dominator)
            {
                continue;
            }

            if (!toto.ContainsKey(dominator))
            {
                toto.Add(dominator, new HashSet<TacInstructionBlock>());
            }

            toto[dominator].Add(dominated);
        }

        RenameBlock(
            entry,
            toto.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToList() as IReadOnlyList<TacInstructionBlock>));
    }

    private void RenameBlock(
        TacInstructionBlock block,
        Dictionary<TacInstructionBlock, IReadOnlyList<TacInstructionBlock>> dominatorTree)
    {
        foreach (var phi in block.Phis)
        {
            phi.Destination.Version = NextVersion(phi.Destination.Name);
        }

        foreach (var instruction in block.Instructions)
        {
            if (instruction.Op is Operand.Phi)
            {
                continue;
            }

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

        foreach (var instruction in block.Instructions.Where(instruction =>
                                                                 instruction.Destination is not null
                                                              && instruction.Op != Operand.Phi))
        {
            _stack[instruction.Destination.Name].Pop();
        }
    }

    private int CurrentVersion(string name)
    {
        return _stack.ContainsKey(name) && _stack[name].Count > 0 ? _stack[name].Peek() : 0;
    }

    private int NextVersion(string name)
    {
        if (!_counter.ContainsKey(name))
        {
            _counter[name] = 0;
            _stack[name] = new Stack<int>();
        }

        var version = ++_counter[name];
        _stack[name].Push(version);
        return version;
    }
}