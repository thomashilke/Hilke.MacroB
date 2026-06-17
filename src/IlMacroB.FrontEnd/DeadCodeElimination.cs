namespace Rollomatic.IlMacroB.FrontEnd;

public class DeadCodeElimination
{
    public DeadCodeElimination(DominatorTree dominanceTree)
    {
        while (RunIteration(dominanceTree))
        {
            ;
        }
    }

    private bool RunIteration(DominatorTree dominanceTree)
    {
        var didChange = false;

        var references = new Dictionary<SsaVariable, DefinitionUses>();
        var substitutions = new Dictionary<SsaVariable, SsaVariable>();

        foreach (var block in dominanceTree.DepthFirstIterator(dominanceTree.Root))
        {
            foreach (var instruction in block.Instructions)
            {
                if (instruction.Destination is SsaVariable destination)
                {
                    if (references.TryGetValue(destination, out var uses))

                    {
                        uses.Definition = instruction;
                        uses.Block = block;
                    }
                    else
                    {
                        references.Add(destination, new DefinitionUses(instruction, block));
                    }

                    if (instruction.Op == Operand.Assign && instruction.Arguments[0] is SsaVariable source)
                    {
                        substitutions[destination] = source;
                    }
                }

                foreach (var (argument, argumentIndex) in instruction.Arguments.Select((instruction, index) =>
                                 (instruction, index)))
                {
                    if (argument is SsaVariable variable)
                    {
                        if (!references.ContainsKey(variable))
                        {
                            // typically for variable that are not defined, such as method arguments, locals, etc.
                            references[variable] = new DefinitionUses();
                        }

                        references[variable].AddUse(argumentIndex, instruction);
                    }
                }
            }
        }

        foreach (var (variable, substitution) in substitutions)
        {
            var definition = references[variable];
            foreach (var use in definition.Uses)
            {
                use.Instruction.Arguments[use.ArgumentIndex] = substitution.Clone();
            }

            definition.Block.RemoveInstruction(definition.Definition);
            didChange = true;
        }

        foreach (var (variables, uses) in references.Where(kvp => !kvp.Value.Uses.Any()))
        {
            // Remove 
            if (uses.Definition.Op is not Operand.Call)
            {
                uses.Block.RemoveInstruction(uses.Definition);
                didChange = true;
            }
            else
            {
                if(uses.Definition.Arguments.First() is FunctionCall functionCall)
                {
                    if (functionCall.IsPure)
                    {
                        uses.Block.RemoveInstruction(uses.Definition);
                    }
                    else
                    {
                        uses.Definition.Destination = null;
                    }
                    didChange = true;
                }
                else
                {
                    throw new InvalidOperationException($"The first argument of a function call should be a FunctionCall, but got {uses.Definition.Arguments.First().GetType()}");
                }
            }
        }

        return didChange;
    }

    public sealed class DefinitionUses
    {
        private readonly List<(int, TacInstruction)> _uses = new();

        public DefinitionUses(TacInstruction definition, TacInstructionBlock block)
        {
            Definition = definition;
            Block = block;
        }

        public DefinitionUses() { }

        public TacInstruction? Definition { get; set; }

        public TacInstructionBlock? Block { get; set; }

        public IReadOnlyList<(int ArgumentIndex, TacInstruction Instruction)> Uses => _uses;

        public void AddUse(int argumentIndex, TacInstruction instruction)
        {
            _uses.Add((argumentIndex, instruction));
        }
    }
}