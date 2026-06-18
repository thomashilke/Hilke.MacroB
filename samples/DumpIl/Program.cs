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

    public static int StartCoolant() => throw new NotSupportedException("Only supported on the CNC.");
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

        CNC.StartCoolant();

        //CNC.Move(a, (CNC.Axis.A, 5.0));

        return Math.Sin(a + b);
    }

    public static int Main(string[] args)
    {
        //var method = typeof(Program).GetMethod(nameof(IterateSomeArray));
        var method = typeof(Program).GetMethod(nameof(AddTwoNumbers));

        var instructions = IlParser.ParseMethod(method);
        var controlFlowGraph = ControlFlowGraphBuilder.Create(instructions);
        var tacControlFlowGraph = TacConverter.Convert(controlFlowGraph);

        var renameTransform = new StaticSingleAssignmentRenameTransform();
        renameTransform.Transform(tacControlFlowGraph);

        var dceTransform = new DeadCodeEliminationTransform();
        dceTransform.Transform(tacControlFlowGraph);

        var constantPropagationTransform = new ConstantPropagatorTransform();
        //constantPropagationTransform.Transform(tacControlFlowGraph);

        var livenessAnalysis = LivenessAnalysis.Analyse(tacControlFlowGraph);

        var livenessRangeAnalysis = LivenessRangeAnalysis.Analyse(tacControlFlowGraph);

        foreach (var block in tacControlFlowGraph.Blocks)
        {
            Console.WriteLine("\n" + GetBlockName(block));
            Console.WriteLine($"Incoming stack: [{string.Join(", ", block.IncomingStack)}]");
            foreach (var instruction in block.Instructions)
            {
                Console.WriteLine(instruction);
            }

            Console.WriteLine($"UeVar: [{string.Join(", ", livenessAnalysis.UeVar[block])}]");
            Console.WriteLine($"VarNotKilled: [{string.Join(", ", livenessAnalysis.VarNotKilled[block])}]");

            Console.WriteLine($"LiveIn: [{string.Join(", ", livenessAnalysis.LiveIn[block])}]");
            Console.WriteLine($"LiveOut: [{string.Join(", ", livenessAnalysis.LiveOut[block])}]");
        }

        Console.WriteLine("");
        foreach (var range in livenessRangeAnalysis.GetRanges())
        {
            Console.WriteLine($"Range: [{string.Join(", ", range)}]");
        }

        return 0;
    }

    private static string GetBlockName(TacInstructionBlock block)
    {
        return $"Block #{block.Id} at offset {block.EntryOffset}";
    }
}