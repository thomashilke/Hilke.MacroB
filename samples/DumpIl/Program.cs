using Rollomatic.IlMacroB.FrontEnd;

namespace Rollomatic.IlMacroB.Samples.DumpIl;

public static class CNC
{
    public enum Axis
    {
        X,
        Y,
        Z,
        A,
        B,
        C
    }

    public static void Move(double feed, params (Axis axis, double position)[] commands)
    {
        throw new NotSupportedException("Only supported on the CNC.");
    }
}

public class Program
{
    public unsafe void IterateSomeArray()
    {
        var toto = stackalloc int[4];

        toto[0] = 1;
        toto[1] = 42;
        toto[2] = 5;
        toto[3] = 22;

        var sum = 0;
        for (var i = 0; i < 4; ++i)
        {
            sum += toto[i];
        }
    }

    public double AddTwoNumbers(int a, int b)
    {
        if (a > 5)
        {
            a -= 5;
        }

        a *= 2;

        //CNC.Move(a, (CNC.Axis.A, 5.0));

        return Math.Sin(a + b);
    }

    public static int Main(string[] args)
    {
        //var method = typeof(Program).GetMethod(nameof(IterateSomeArray));
        var method = typeof(Program).GetMethod(nameof(AddTwoNumbers));

        var instructions = IlParser.ParseMethod(method);
        var controlFlowGraph = ControlFlowGraphBuilder.BuildControlFlowGraph(instructions);
        var tacBlocks = TacConverter.Convert(controlFlowGraph);
        SsaRenamer.Rename(tacBlocks);

        foreach (var block in tacBlocks)
        {
            Console.WriteLine("\n" + GetBlockName(block.BasicBlock));
            Console.WriteLine($"Incoming stack: [{string.Join(", ", block.IncomingStack)}]");
            foreach (var instruction in block.Instructions)
            {
                Console.WriteLine(instruction);
            }
        }

        return 0;
    }

    private static string GetBlockName(BasicBlock block)
    {
        return $"Block #{block.Id} at offset {block.Instructions.First().Offset}";
    }
}