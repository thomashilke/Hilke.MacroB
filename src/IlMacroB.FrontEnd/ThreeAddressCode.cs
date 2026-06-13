using System.Reflection;

namespace Rollomatic.IlMacroB.FrontEnd;

public class TacInstructionBlock
{
    public TacInstructionBlock(BasicBlock basicBlock, IEnumerable<TacInstruction> instructions, TacInstruction? branchInstruction, List<string> incomingStack, List<string> outgoingStack)
    {
        BasicBlock = basicBlock ?? throw new ArgumentNullException(nameof(basicBlock));

        Instructions = instructions ?? throw new ArgumentNullException(nameof(instructions));
        BranchInstruction = branchInstruction;

        IncomingStack = incomingStack ?? throw new ArgumentNullException(nameof(incomingStack));
        OutgoingStack = outgoingStack ?? throw new ArgumentNullException(nameof(outgoingStack));
    }

    public BasicBlock BasicBlock { get; }
    public IEnumerable<TacInstruction> Instructions { get; }
    public TacInstruction? BranchInstruction { get; }
    public List<string> IncomingStack { get; }

    public List<string> OutgoingStack { get; }
}

public class TacInstruction
{
    public string Destination { get; set; }
    public string Op { get; set; }
    public string Arg1 { get; set; }
    public string Arg2 { get; set; }

    public bool IsBranch { get; set; }

    public override string ToString() =>
        !string.IsNullOrEmpty(Destination)
        ? $"{Destination} = {Arg1} {Op} {Arg2}".Trim()
        : $"{Arg1} {Op} {Arg2}".Trim();
}

public class BlockTranslationContext
{
    public List<string>? IncomingStack { get; internal set; }

    public List<string>? OutgoingStack { get; internal set; }
}

public static class TacConverter
{
    public static IEnumerable<TacInstructionBlock> DoTheFixedPointIterationToFindStackShapes(ControlFlowGraph controlFlowGraph)
    {
        var reversePostOrder = controlFlowGraph
            .ReversePostOrder(controlFlowGraph.InitialBlock)
            .ToList();

        var context = reversePostOrder.ToDictionary(block => block, _ => new BlockTranslationContext());
        context[controlFlowGraph.InitialBlock].IncomingStack = new(); // empty initial stack

        var changed = true;
        while (changed)
        {
            changed = false;

            foreach (var block in reversePostOrder)
            {
                if (block.Predecessors.Any())
                {
                    var newIncomingStack = Merge(block.Predecessors.Select(predecessor => context[predecessor].OutgoingStack));

                    if (context[block].IncomingStack is null ||
                        newIncomingStack is not null && newIncomingStack.Count != context[block].IncomingStack.Count)
                    {
                        context[block].IncomingStack = newIncomingStack;
                        changed = true;
                    }
                }

                ConvertBlockToTac(block, context[block].IncomingStack, out var newOutgoingStack);

                if (context[block].OutgoingStack is null || newOutgoingStack.Count != context[block].OutgoingStack.Count)
                {
                    context[block].OutgoingStack = newOutgoingStack;
                    changed = true;
                }
            }
        }

        var tacBlocks = controlFlowGraph.Blocks.Select(block =>
        {
            var incomingStack = context[block].IncomingStack;
            var tacInstructions =  ConvertBlockToTac(block, incomingStack, out var outgoingStack);

            if (tacInstructions.Last().IsBranch)
            {
                return new TacInstructionBlock(block, tacInstructions.SkipLast(1), tacInstructions.Last(), incomingStack, outgoingStack);
            }
            else
            {
                return new TacInstructionBlock(block, tacInstructions, null, incomingStack, outgoingStack);
            }
        });

        return tacBlocks;
    }

    private static List<string>?  Merge(IEnumerable<List<string>?> enumerable)
    {
        var stacks = enumerable.OfType<List<string>>().DistinctBy(stack => stack.Count());

        if (stacks.Any())
        {
            return stacks.Single();
        }

        return null;
    }

    public static List<TacInstruction> ConvertBlockToTac(
        BasicBlock block,
        List<string> incomingStack,
        out List<string> outgoingStack)
    {
        var tacInstructions = new List<TacInstruction>();
        var evaluationStack = new Stack<string>(incomingStack.AsEnumerable().Reverse());
        var tempCounter = 0;

        foreach (var instruction in block.Instructions)
        {
            HandleInstruction(instruction);
        }

        outgoingStack = new();
        var remainingStackItems = evaluationStack.ToList();

        for (var i = remainingStackItems.Count - 1; i >= 0; i--)
        {
            var slotName = $"stack_slot_{i}";
            tacInstructions.Add(new() { Destination = slotName, Op = "assign", Arg1 = remainingStackItems[i] });
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
                var source = instruction.Operand?.ToString() ?? "loc_" + opName.Substring(opName.IndexOf(".") + 1);
                evaluationStack.Push(source);
            }
            else if (opName.StartsWith("ldarg"))
            {
                var source = instruction.Operand?.ToString() ?? "arg_" + opName.Substring(opName.IndexOf(".") + 1);
                evaluationStack.Push(source);
            }
            else if (opName.StartsWith("ldc"))
            {
                var source = instruction.Operand?.ToString() ?? opName.Substring(opName.IndexOf(".") + 1);
                evaluationStack.Push(source);
            }
            else if (opName.StartsWith("stloc"))
            {
                var destination = instruction.Operand?.ToString() ?? opName.Substring(opName.IndexOf(".") + 1);
                var value = evaluationStack.Count > 0 ? evaluationStack.Pop() : throw new InvalidOperationException();
                tacInstructions.Add(new() { Destination = $"loc_{destination}", Op = "assign", Arg1 = value });
            }
            else if (opName.StartsWith("starg"))
            {
                var destination = instruction.Operand?.ToString() ?? opName.Substring(opName.IndexOf(".") + 1);
                var value = evaluationStack.Count > 0 ? evaluationStack.Pop() : throw new InvalidOperationException();
                tacInstructions.Add(new() { Destination = $"arg_{destination}", Op = "assign", Arg1 = value });
            }
            else if (opName == "add" || opName == "sub" || opName == "mul" || opName == "div" || opName == "cgt")
            {
                var right = evaluationStack.Pop();
                var left = evaluationStack.Pop();
                var tempRegister = $"t{tempCounter++}";

                tacInstructions.Add(new() { Destination = tempRegister, Arg1 = left, Op = opName, Arg2 = right });
                evaluationStack.Push(tempRegister);
            }
            else if (instruction.AbsoluteTarget.HasValue)
            {
                if (opName.StartsWith("brtrue") || opName.StartsWith("brfalse"))
                {
                    var condition = evaluationStack.Pop();
                    tacInstructions.Add(new() { Op = "goto_if_" + opName, Arg1 = condition, Arg2 = $"Block_at_{instruction.AbsoluteTarget.Value}", IsBranch = true });
                }
                else if (opName.StartsWith("ble") || opName.StartsWith("bgt") || opName.StartsWith("bge") || opName.StartsWith("blt") || opName.StartsWith("beq") || opName.StartsWith("bne"))
                {
                    var left = evaluationStack.Pop();
                    var right = evaluationStack.Pop();

                    tacInstructions.Add(new() { Destination = $"Block_at_{instruction.AbsoluteTarget.Value}", Op = opName, Arg1 = left, Arg2 = right, IsBranch = true });
                }
                else if (opName.StartsWith("br"))
                { // unconditional jump
                    tacInstructions.Add(new() { Op = "goto", Arg1 = $"Block_at_{instruction.AbsoluteTarget.Value}", IsBranch = true });
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
                var arguments = new List<string>();

                for (var i = 0; i < parameterCount; ++i)
                {
                    arguments.Insert(0, evaluationStack.Pop());
                }

                var argumentsAsString = string.Join(", ", arguments);
                var callTarget = methodInfo?.Name ?? "DynamicCall";

                if (opName.Contains("void") || methodInfo is MethodInfo mi && mi.ReturnType == typeof(void))
                {
                    tacInstructions.Add(new() { Op = "call", Arg1 = callTarget, Arg2 = argumentsAsString });
                }
                else
                {
                    var tempRegister = $"t{tempCounter++}";
                    tacInstructions.Add(new() { Destination = tempRegister, Op = "call", Arg1 = callTarget, Arg2 = argumentsAsString });
                    evaluationStack.Push(tempRegister);
                }
            }
            else if (opName == "ret")
            {
                var retVal = evaluationStack.Count > 0
                    ? evaluationStack.Pop()
                    : throw new InvalidOperationException();

                tacInstructions.Add(new() { Op = "return", Arg1 = retVal });
            }
            else if (opName == "nop")
            {

            }
            else
            {
                throw new NotSupportedException($"Unsupported instruction {opName}");
            }
        }
    }
}
