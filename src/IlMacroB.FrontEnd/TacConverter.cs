using System.Reflection;

namespace Rollomatic.IlMacroB.FrontEnd;

public static class TacConverter
{
    public static IReadOnlyList<TacInstructionBlock> Convert(ControlFlowGraph controlFlowGraph)
    {
        var reversePostOrder = controlFlowGraph
                               .ReversePostOrder(controlFlowGraph.InitialBlock)
                               .ToList();

        var context = reversePostOrder.ToDictionary(block => block, _ => new BlockTranslationContext());
        context[controlFlowGraph.InitialBlock].IncomingStack = new List<string>(); // empty initial stack

        var changed = true;
        while (changed)
        {
            changed = false;

            foreach (var block in reversePostOrder)
            {
                if (block.Predecessors.Any())
                {
                    var newIncomingStack = Merge(
                        block.Predecessors.Select(predecessor => context[predecessor].OutgoingStack));

                    if (context[block].IncomingStack is null
                     || (newIncomingStack is not null && newIncomingStack.Count != context[block].IncomingStack.Count))
                    {
                        context[block].IncomingStack = newIncomingStack;
                        changed = true;
                    }
                }

                ConvertBlockToTac(block, context[block].IncomingStack, out var newOutgoingStack);

                if (context[block].OutgoingStack is null
                 || newOutgoingStack.Count != context[block].OutgoingStack.Count)
                {
                    context[block].OutgoingStack = newOutgoingStack;
                    changed = true;
                }
            }
        }

        var tacBlocks = controlFlowGraph.Blocks.Select(block =>
                                        {
                                            var incomingStack = context[block].IncomingStack;
                                            var tacInstructions = ConvertBlockToTac(
                                                block,
                                                incomingStack,
                                                out var outgoingStack);

                                            if (tacInstructions.Last().IsBranch)
                                            {
                                                return new TacInstructionBlock(
                                                    block,
                                                    tacInstructions.SkipLast(1),
                                                    tacInstructions.Last(),
                                                    incomingStack,
                                                    outgoingStack);
                                            }

                                            return new TacInstructionBlock(
                                                block,
                                                tacInstructions,
                                                new TacInstruction(
                                                    Operand.Br,
                                                    $"Block_at_{block.Instructions.Last().NextInstructionOffset}"),
                                                incomingStack,
                                                outgoingStack);
                                        })
                                        .ToList();

        var blockMap = tacBlocks.ToDictionary(b => b.BasicBlock);

        foreach (var tacBlock in tacBlocks)
        {
            tacBlock.Predecessors = tacBlock.BasicBlock.Predecessors.Select(p => blockMap[p]).ToList();
            tacBlock.Successors = tacBlock.BasicBlock.Successors.Select(p => blockMap[p]).ToList();
        }

        return tacBlocks;
    }

    public static List<TacInstruction> ConvertBlockToTac(
        BasicBlock block,
        List<string> incomingStack,
        out List<string> outgoingStack)
    {
        var tacInstructions = new List<TacInstruction>();
        var evaluationStack = new Stack<object>(incomingStack.AsEnumerable().Reverse().Select(n => new SsaVariable(n)));
        var tempCounter = 0;

        foreach (var instruction in block.Instructions)
        {
            HandleInstruction(instruction);
        }

        outgoingStack = new List<string>();
        var remainingStackItems = evaluationStack.ToList();

        for (var i = remainingStackItems.Count - 1; i >= 0; i--)
        {
            var slotName = $"stack_slot_{i}";
            tacInstructions.Add(new TacInstruction(slotName, Operand.Assign, remainingStackItems[i]));
            outgoingStack.Insert(0, slotName);
        }

        if (block.BranchInstruction is not null)
        {
            HandleInstruction(block.BranchInstruction);
        }

        return tacInstructions;

        void HandleInstruction(IlInstruction instruction)
        {
            var opName = instruction.OpCode.Name;

            if (opName.StartsWith("ldloc"))
            {
                var source = instruction.Operand?.ToString()
                          ?? opName.Substring(opName.IndexOf(".") + 1);

                evaluationStack.Push(new SsaVariable("loc_" + source));
            }
            else if (opName.StartsWith("ldarg"))
            {
                var source = instruction.Operand?.ToString()
                          ?? opName.Substring(opName.IndexOf(".") + 1);

                evaluationStack.Push(new SsaVariable("arg_" + source));
            }
            else if (opName.StartsWith("ldc"))
            {
                var source = instruction.Operand?.ToString()
                          ?? opName.Substring(opName.IndexOf(".") + 1);

                evaluationStack.Push(source);
            }
            else if (opName.StartsWith("stloc"))
            {
                var destination = instruction.Operand?.ToString() ?? opName.Substring(opName.IndexOf(".") + 1);
                var value = evaluationStack.Count > 0 ? evaluationStack.Pop() : throw new InvalidOperationException();
                tacInstructions.Add(new TacInstruction($"loc_{destination}", Operand.Assign, value));
            }
            else if (opName.StartsWith("starg"))
            {
                var destination = instruction.Operand?.ToString() ?? opName.Substring(opName.IndexOf(".") + 1);
                var value = evaluationStack.Count > 0 ? evaluationStack.Pop() : throw new InvalidOperationException();
                tacInstructions.Add(new TacInstruction($"arg_{destination}", Operand.Assign, value));
            }
            else if (opName == "add" || opName == "sub" || opName == "mul" || opName == "div" || opName == "cgt")
            {
                var right = evaluationStack.Pop();
                var left = evaluationStack.Pop();
                var tempRegister = $"t{tempCounter++}";

                tacInstructions.Add(new TacInstruction(tempRegister, Enum.Parse<Operand>(opName, true), left, right));
                evaluationStack.Push(new SsaVariable(tempRegister));
            }
            else if (instruction.AbsoluteTarget.HasValue)
            {
                if (opName.StartsWith("brtrue") || opName.StartsWith("brfalse"))
                {
                    var condition = evaluationStack.Pop();
                    tacInstructions.Add(
                        new TacInstruction(
                            Enum.Parse<Operand>(opName.Substring(0, opName.IndexOf('.')), true),
                            condition,
                            $"Block_at_{instruction.AbsoluteTarget.Value}",
                            $"Block_at_{instruction.NextInstructionOffset}"));
                }
                else if (opName.StartsWith("ble")
                      || opName.StartsWith("bgt")
                      || opName.StartsWith("bge")
                      || opName.StartsWith("blt")
                      || opName.StartsWith("beq")
                      || opName.StartsWith("bne"))
                {
                    var left = evaluationStack.Pop();
                    var right = evaluationStack.Pop();

                    var name = opName.Substring(0, opName.IndexOf('.'));

                    tacInstructions.Add(
                        new TacInstruction(
                            Enum.Parse<Operand>(name, true),
                            left,
                            right,
                            $"Block_at_{instruction.AbsoluteTarget.Value}",
                            $"Block_at_{instruction.NextInstructionOffset}"));
                }
                else if (opName.StartsWith("br"))
                {
                    // unconditional jump
                    tacInstructions.Add(new TacInstruction(Operand.Br, $"Block_at_{instruction.AbsoluteTarget.Value}"));
                }
                else
                {
                    throw new NotSupportedException($"Unsupported instruction: {opName}");
                }
            }
            else if (opName.StartsWith("call"))
            {
                var methodInfo = instruction.Operand as MethodBase;
                var parameterCount = methodInfo?.GetParameters().Length ?? 0;
                var arguments = new List<object>();

                for (var i = 0; i < parameterCount; ++i)
                {
                    arguments.Insert(0, evaluationStack.Pop());
                }

                var callTarget = methodInfo?.Name ?? "DynamicCall";

                if (opName.Contains("void") || (methodInfo is MethodInfo mi && mi.ReturnType == typeof(void)))
                {
                    tacInstructions.Add(
                        new TacInstruction(
                            Operand.Call,
                            arguments.Prepend(callTarget).ToArray()));
                }
                else
                {
                    var tempRegister = $"t{tempCounter++}";

                    tacInstructions.Add(
                        new TacInstruction(
                            tempRegister,
                            Operand.Call,
                            arguments.Prepend(callTarget).ToArray()));

                    evaluationStack.Push(new SsaVariable(tempRegister));
                }
            }
            else if (opName == "ret")
            {
                var retVal = evaluationStack.Count > 0
                                 ? evaluationStack.Pop()
                                 : throw new InvalidOperationException();

                tacInstructions.Add(new TacInstruction(Operand.Ret, retVal));
            }
            else if (opName == "nop")
            {
                // Ignore nop
            }
            else if (opName.StartsWith("conv"))
            {
                // Ignore conversion
            }
            else
            {
                throw new NotSupportedException($"Unsupported instruction {opName}");
            }
        }
    }

    private static List<string>? Merge(IEnumerable<List<string>?> enumerable)
    {
        var stacks = enumerable.OfType<List<string>>().DistinctBy(stack => stack.Count());

        if (stacks.Any())
        {
            return stacks.Single();
        }

        return null;
    }
}