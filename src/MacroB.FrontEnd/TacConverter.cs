using System.Reflection;
using System.Reflection.Emit;

namespace Hilke.MacroB.FrontEnd;

public static class TacConverter
{
    public static ControlFlowGraph<TacInstructionBlock> Convert(ControlFlowGraph<BasicBlock> controlFlowGraph)
    {
        var reversePostOrder = controlFlowGraph
                               .ReversePostOrder(controlFlowGraph.EntryBlock)
                               .ToList();

        var context = reversePostOrder.ToDictionary(block => block, _ => new BlockTranslationContext());
        context[controlFlowGraph.EntryBlock].IncomingStack = new List<OperandBase>(); // empty initial stack

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

        var betterStackComputation = InOutStackAnalysis.Analyse(controlFlowGraph, (block, inStack) => {
            ConvertBlockToTac(block, inStack.Stack ?? new(), out var outStack);
            return new(outStack);
        });

        var tacBlocks = controlFlowGraph.Blocks.ToDictionary(
            block => block,
            block => ConvertBlockToTac(
                block,
                context[block].IncomingStack,
                out var _));

        var blockMap = tacBlocks.ToDictionary(b => b.Value, b => b.Key);

        foreach (var tacBlock in tacBlocks.Values)
        {
            tacBlock.AddPredecessors(blockMap[tacBlock].Predecessors.Select(p => tacBlocks[p]).ToList());
            tacBlock.AddSuccessors(blockMap[tacBlock].Successors.Select(p => tacBlocks[p]).ToList());
        }

        return new ControlFlowGraph<TacInstructionBlock>(tacBlocks.Values.ToList(), tacBlocks[controlFlowGraph.EntryBlock]);
    }

    public static TacInstructionBlock ConvertBlockToTac(
        BasicBlock block,
        List<OperandBase> incomingStack,
        out List<OperandBase> outgoingStack)
    {
        var tacInstructions = new List<TacInstruction>();
        var evaluationStack = new Stack<OperandBase>(incomingStack.AsEnumerable().Reverse());
        var tempCounter = 0;

        foreach (var instruction in block.BodyInstructions)
        {
            if (HandleInstruction(instruction, ref evaluationStack, ref tempCounter, out var tacInstruction))
            {
                tacInstructions.Add(tacInstruction);
            }
        }

        if (block.BranchInstruction is not null)
        {
            if (HandleInstruction(block.BranchInstruction, ref evaluationStack, ref tempCounter, out var branchInstruction))
            {
                outgoingStack = SpillStackVariables(ref tacInstructions, evaluationStack);

                return new TacInstructionBlock(
                    block.Id, block.BodyInstructions.First().Offset, block.IsInitial,
                    tacInstructions, branchInstruction);
            }
        }

        outgoingStack = SpillStackVariables(ref tacInstructions, evaluationStack);

        var unconditionalBranch = new TacInstruction(
            Operand.Br,
            new JumpTarget(block.Instructions.Last().NextInstructionOffset));

        return new TacInstructionBlock(block.Id, block.BodyInstructions.First().Offset, block.IsInitial,
                                       tacInstructions, unconditionalBranch);
    }

    private static bool HandleInstruction(
        IlInstruction instruction,
        ref Stack<OperandBase> evaluationStack,
        ref int tempCounter,
        out TacInstruction? tacInstruction)
    {
        var opName = instruction.OpCode.Name;

        if (opName.StartsWith("ldloc"))
        {
            var source = instruction.Operand?.ToString()
                      ?? opName.Substring(opName.IndexOf(".") + 1);

            evaluationStack.Push(new SsaVariable("loc_" + source));

            tacInstruction = null;
            return false;
        }
        else if (opName.StartsWith("ldarg"))
        {
            var source = instruction.Operand?.ToString()
                      ?? opName.Substring(opName.IndexOf(".") + 1);

            evaluationStack.Push(new SsaVariable("arg_" + source));

            tacInstruction = null;
            return false;
        }
        else if (opName.StartsWith("ldc"))
        {
            if (instruction.OpCode == OpCodes.Ldc_I4)
            {
                evaluationStack.Push(new Constant(instruction.Operand.ToString()));
            }
            else if (instruction.OpCode == OpCodes.Ldc_I8)
            {
                evaluationStack.Push(new Constant(instruction.Operand.ToString()));
            }
            else if (instruction.OpCode == OpCodes.Ldc_I4_0)
            {
                evaluationStack.Push(new Constant("0"));
            }
            else if (instruction.OpCode == OpCodes.Ldc_I4_1)
            {
                evaluationStack.Push(new Constant("1"));
            }
            else if (instruction.OpCode == OpCodes.Ldc_I4_2)
            {
                evaluationStack.Push(new Constant("2"));
            }
            else if (instruction.OpCode == OpCodes.Ldc_I4_3)
            {
                evaluationStack.Push(new Constant("3"));
            }
            else if (instruction.OpCode == OpCodes.Ldc_I4_4)
            {
                evaluationStack.Push(new Constant("4"));
            }
            else if (instruction.OpCode == OpCodes.Ldc_I4_5)
            {
                evaluationStack.Push(new Constant("5"));
            }
            else if (instruction.OpCode == OpCodes.Ldc_I4_6)
            {
                evaluationStack.Push(new Constant("6"));
            }
            else if (instruction.OpCode == OpCodes.Ldc_I4_7)
            {
                evaluationStack.Push(new Constant("7"));
            }
            else if (instruction.OpCode == OpCodes.Ldc_I4_8)
            {
                evaluationStack.Push(new Constant("8"));
            }
            else if (instruction.OpCode == OpCodes.Ldc_I4_M1)
            {
                evaluationStack.Push(new Constant("-1"));
            }
            else if (instruction.OpCode == OpCodes.Ldc_I4_S)
            {
                evaluationStack.Push(new Constant(instruction.Operand.ToString()));
            }
            else if (instruction.OpCode == OpCodes.Ldc_R4)
            {
                evaluationStack.Push(new Constant(instruction.Operand.ToString()));
            }
            else if (instruction.OpCode == OpCodes.Ldc_R8)
            {
                evaluationStack.Push(new Constant(instruction.Operand.ToString()));
            }

            tacInstruction = null;
            return false;
        }
        else if (opName.StartsWith("stloc"))
        {
            var destination = instruction.Operand?.ToString() ?? opName.Substring(opName.IndexOf(".") + 1);
            var value = evaluationStack.Count > 0 ? evaluationStack.Pop() : throw new InvalidOperationException();
            tacInstruction = new TacInstruction($"loc_{destination}", Operand.Assign, value);

            return true;
        }
        else if (opName.StartsWith("starg"))
        {
            var destination = instruction.Operand?.ToString() ?? opName.Substring(opName.IndexOf(".") + 1);
            var value = evaluationStack.Count > 0 ? evaluationStack.Pop() : throw new InvalidOperationException();

            tacInstruction = new TacInstruction($"arg_{destination}", Operand.Assign, value);
            return true;
        }
        else if (opName == "add" || opName == "sub" || opName == "mul" || opName == "div" || opName == "cgt" || opName == "clt")
        {
            var right = evaluationStack.Pop();
            var left = evaluationStack.Pop();
            var tempRegister = $"t{tempCounter++}";

            tacInstruction = new TacInstruction(tempRegister, Enum.Parse<Operand>(opName, true), left, right);
            evaluationStack.Push(new SsaVariable(tempRegister));
            return true;
        }
        else if (instruction.AbsoluteTarget.HasValue)
        {
            if (opName.StartsWith("brtrue") || opName.StartsWith("brfalse"))
            {
                var condition = evaluationStack.Pop();
                tacInstruction =
                    new TacInstruction(
                        Enum.Parse<Operand>(opName.Substring(0, opName.IndexOf('.')), true),
                        condition,
                        new JumpTarget(instruction.AbsoluteTarget.Value),
                        new JumpTarget(instruction.NextInstructionOffset));

                return true;
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

                tacInstruction =
                    new TacInstruction(
                        Enum.Parse<Operand>(name, true),
                        left,
                        right,
                        new JumpTarget(instruction.AbsoluteTarget.Value),
                        new JumpTarget(instruction.NextInstructionOffset));

                return true;
            }
            else if (opName.StartsWith("br"))
            {
                tacInstruction =
                    new TacInstruction(Operand.Br, new JumpTarget(instruction.AbsoluteTarget.Value));

                return true;
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
            var arguments = new List<OperandBase>();

            for (var i = 0; i < parameterCount; ++i)
            {
                arguments.Insert(0, evaluationStack.Pop());
            }

            var callTarget = methodInfo?.Name ?? "DynamicCall";

            if (opName.Contains("void") || (methodInfo is MethodInfo mi && mi.ReturnType == typeof(void)))
            {
                tacInstruction =
                    new TacInstruction(
                        Operand.Call,
                        arguments.Prepend(new IntrinsicFunctionCall(callTarget, false)).ToArray());

                return true;
            }
            else
            {
                var tempRegister = $"t{tempCounter++}";
                evaluationStack.Push(new SsaVariable(tempRegister));

                if (callTarget == "Sin")
                {
                    tacInstruction = new TacInstruction(tempRegister, Operand.Sin, arguments.ToArray());
                }
                else
                {
                    tacInstruction =
                        new TacInstruction(
                            tempRegister,
                            Operand.Call,
                            arguments.Prepend(new IntrinsicFunctionCall(callTarget, false)).ToArray());
                }

                return true;
            }
        }
        else if (opName == "ret")
        {
            var retVal = evaluationStack.Count > 0
                             ? evaluationStack.Pop()
                             : null;

            tacInstruction =
                retVal is not null
                ? new TacInstruction(Operand.Ret, retVal)
                : new TacInstruction(Operand.Ret);

            return true;
        }
        else if (opName == "pop")
        {
            evaluationStack.Pop();
            tacInstruction = null;
            return false;
        }
        else if (opName == "nop")
        {
            // Ignore nop
            tacInstruction = null;
            return false;
        }
        else if (opName.StartsWith("conv"))
        {
            // Ignore conversion
            tacInstruction = null;
            return false;
        }
        else
        {
            throw new NotSupportedException($"Unsupported instruction {opName}");
        }
    }

    private static List<OperandBase> SpillStackVariables(
        ref List<TacInstruction> tacInstructions,
        Stack<OperandBase> evaluationStack)
    {
        var remainingStackItems = evaluationStack.ToList();
        var outgoingStack = new List<OperandBase>();

        for (var i = remainingStackItems.Count - 1; i >= 0; i--)
        {
            var slotName = $"stack_slot_{i}";
            tacInstructions.Add(new TacInstruction(slotName, Operand.Assign, remainingStackItems[i]));
            outgoingStack.Insert(0, new SsaVariable(slotName));
        }

        return outgoingStack;
    }

    private static List<OperandBase>? Merge(IEnumerable<List<OperandBase>?> enumerable)
    {
        var stacks = enumerable.OfType<List<OperandBase>>().DistinctBy(stack => stack.Count());

        if (stacks.Any())
        {
            return stacks.Single();
        }

        return null;
    }
}
