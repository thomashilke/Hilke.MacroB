namespace Rollomatic.IlMacroB.FrontEnd;

public class DeadCodeEliminationTransform : IControlFlowGraphTransformation<TacInstructionBlock>
{
    /// <inheritdoc />
    public void Transform(ControlFlowGraph<TacInstructionBlock> controlFlowGraph)
    {
        var dominanceTree = DominanceEngine.ComputeDominatorTree(controlFlowGraph.Blocks);

        while (RunIteration(dominanceTree))
        {
            ;
        }
    }

    private bool RunIteration(DominatorTree dominanceTree)
    {
        var didChange = false;

        var references = new Dictionary<SsaVariable, VariableDefinitionUses>();
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
                        references.Add(destination, new VariableDefinitionUses(instruction, block));
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
                            references[variable] = new VariableDefinitionUses();
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
                if (uses.Definition.Arguments.First() is FunctionCall functionCall)
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
                    throw new InvalidOperationException(
                        $"The first argument of a function call should be a FunctionCall, but got {uses.Definition.Arguments.First().GetType()}");
                }
            }
        }

        return didChange;
    }
}