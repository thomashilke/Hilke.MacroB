namespace Rollomatic.IlMacroB.FrontEnd;

public class InOutStackAnalysis
{
    public InOutStackAnalysis(Dictionary<BasicBlock, InStack> inStacks)
    {
        InStacks = inStacks;
    }

    public Dictionary<BasicBlock, InStack> InStacks { get; }

    public static InOutStackAnalysis Analyse(
        ControlFlowGraph<BasicBlock> controlFlowGraph,
        Func<BasicBlock, InStack, InStack> transferDelegate)
    {
        var solver = new DataFlowAnalysisSolver<InStack, BasicBlock>(
            InitializeDelegate,
            transferDelegate,
            MergeDelegate);

        return new InOutStackAnalysis(solver.SolveForwardFlow(controlFlowGraph));
    }

    private static InStack MergeDelegate(IEnumerable<InStack> enumerable)
    {
        var stacks = enumerable.Where(inStack => inStack.Stack is not null)
                               .DistinctBy(inStack => inStack.Stack.Count());

        if (stacks.Any())
        {
            return stacks.Single();
        }

        return new(new List<OperandBase>());
    }

    private static InStack InitializeDelegate(BasicBlock block)
    {
        return new InStack();
    }

    public sealed class InStack: IEquatable<InStack>
    {
        public InStack() { }

        public InStack(List<OperandBase> stack)
        {
            Stack = stack ?? throw new ArgumentNullException(nameof(stack));
        }

        public List<OperandBase>? Stack { get; }

        public bool Equals(InStack? other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (Stack is null && other.Stack is null)
            {
                return true;
            }

            if (Stack is not null && other.Stack is not null)
            {
                if (Stack.Count != other.Stack.Count)
                {
                    return false;
                }

                return Stack.Zip(other.Stack, (a, b) => a.Equals(b)).All(x => x);
            }

            return false;
        }

        public override bool Equals(object? obj)
        {
            if (obj is InStack inStack)
            {
                return Equals(inStack);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return Stack.Aggregate(0, (current, next) => HashCode.Combine(current, next.GetHashCode()));
        }

        public static bool operator ==(InStack stack1, InStack stack2) { return stack1.Equals(stack2); }
        public static bool operator !=(InStack stack1, InStack stack2) { return !stack1.Equals(stack2); }
    }
}
