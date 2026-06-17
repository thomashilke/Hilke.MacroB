namespace Rollomatic.IlMacroB.FrontEnd;

public static class ConstantPropagator
{
    public static void PropagateConstants(DominatorTree dominatorTree)
    {
        var didChange = true;

        var substitutions = new Dictionary<SsaVariable, SsaVariable>() as IDictionary<SsaVariable, SsaVariable>;

        while (didChange)
        {
            didChange &= SubstituteInBlock(dominatorTree.DepthFirstIterator(dominatorTree.Root), ref substitutions);
        }
    }

    private static bool SubstituteInBlock(
        IEnumerable<TacInstructionBlock> blocks,
        ref IDictionary<SsaVariable, SsaVariable> substitutions)
    {
        var didSubstitute = false;

        foreach (var block in blocks)
        {
            foreach (var instruction in block.Instructions)
            {
                if (instruction.Op == Operand.Assign && instruction.Arguments[0] is SsaVariable variable)
                {
                    substitutions[instruction.Destination] = variable;
                }
                else
                {
                    // From there we should eliminate the instruction in the block, and for each subsequent block we should 
                    // search through all arguments, and apply the substitution for each of them.

                    instruction.Arguments = ApplySubstitution(
                        instruction.Arguments,
                        substitutions,
                        out var didSubstituteInArgs);
                    didSubstitute |= didSubstituteInArgs;
                }
            }
        }

        return didSubstitute;
    }

    private static List<object> ApplySubstitution(
        IEnumerable<object> arguments,
        IDictionary<SsaVariable, SsaVariable> substitutions,
        out bool didSubstitute)
    {
        var args = arguments
                   .Select(argument => TrySubstitute(argument, substitutions))
                   .Aggregate(
                       (new List<object>(), false),
                       (accumulate, next) =>
                       {
                           accumulate.Item1.Add(next.Item1);

                           return (accumulate.Item1, accumulate.Item2 || next.Item2);
                       });

        didSubstitute = args.Item2;
        return args.Item1;
    }

    private static (object, bool) TrySubstitute(object argument, IDictionary<SsaVariable, SsaVariable> substitutions)
    {
        if (argument is SsaVariable variable && substitutions.TryGetValue(variable, out var value))
        {
            return (value.Clone(), true);
        }

        return (argument, false);
    }
}