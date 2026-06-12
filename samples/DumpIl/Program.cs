using Rollomatic.IlMacroB.FrontEnd;

namespace Rollomatic.IlMacroB.Samples.DumpIl;

public class Program
{
    public int AddTwoNumbers(int a, int b)
    {
        if (a > 5)
        {
            a -= 5;
        }

        return a + b;
    }

    public static int Main(string[] args)
    {
        var method = typeof(Program).GetMethod(nameof(AddTwoNumbers));
        var instructions = IlParser.ParseMethod(method);

        foreach (var instruction in instructions)
        {
            Console.WriteLine(instruction);
        }

        var controlFlowGraph = ControlFlowGraphBuilder.BuildControlFlowGraph(instructions);

        foreach (var block in controlFlowGraph.Blocks)
        {
            Console.WriteLine($"\n{GetBlockName(block)}");
            foreach (var instruction in block.Instructions)
            {
                Console.WriteLine(instruction);
            }
            Console.WriteLine($"Successors: {string.Join(", ", block.Successors.Select(GetBlockName))}");
        }

        var tacBlocks = TacConverter.DoTheFixedPointIterationToFindStackShapes(controlFlowGraph);

        foreach (var block in tacBlocks)
        {
            Console.WriteLine("\n" + GetBlockName(block.BasicBlock));
            Console.WriteLine($"Incoming stack: [{string.Join(", ", block.IncomingStack)}]");
            foreach (var instruction in block.Instructions)
            {
                Console.WriteLine(instruction);
            }
            Console.WriteLine($"Outgoing stack: [{string.Join(", ", block.OutgoingStack)}]");
        }

        return 1;
    }

    private static string GetBlockName(BasicBlock block)
    {
        return $"Block_{block.Instructions.First().Offset}";
    }
}
