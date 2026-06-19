using IlMacroB.Host.FrontEnd;

using Rollomatic.IlMacroB.FrontEnd;

namespace Rollomatic.IlMacroB.Samples.DumpIl;

// ToDo: 1. Merge consecutive control flow blocks
//       2. Rebuild mathematical expressions
//       4. Detect and reconstruct IF-block (Strong Connected Components?)
//       6. Implement all mathematical operators,
//       8. Logging?
//       9. Debugging?
//       10. Synchronous communication chanel
//       11. Read/Write ordering?
//       12. Simulation/Unit testing?

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

    public static void IsoProgramDemo(int a, int b)
    {
        if (a > 5)
        {
            a -= 5;
        }

        a *= 2;

        Cnc.Machine.StartCoolant();
        Cnc.Machine.Move(
            feed: 500.0,
            x: Cnc.Math.Sin(a + b),
            y: 2.0,
            z: 34.0);
        Cnc.Machine.StopCoolant();
    }

    public static int Main(string[] args)
    {
        Demo();

        return 0;
    }

    public static void Demo()
    {
        //var method = typeof(Program).GetMethod(nameof(IterateSomeArray));
        var method = typeof(Program).GetMethod(nameof(IsoProgramDemo));

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
        var livenessRangeInterferenceAnalysis = LivenessRangeInterferenceAnalysis.Analyse(tacControlFlowGraph);
        var coloring = livenessRangeInterferenceAnalysis.LivenessRangeInterferencesGraph.GreedyColoring();
        var blockOrderAnalysis = BasicBlockOrderAnalysis.Analyse(tacControlFlowGraph, new NaiveCostModel());

        foreach (var block in blockOrderAnalysis.BlockOrder)
        {
            Console.WriteLine("\n" + GetBlockName(block));

            //Console.WriteLine($"Incoming stack: [{string.Join(", ", block.IncomingStack)}]");
            foreach (var instruction in block.Instructions)
            {
                Console.WriteLine(instruction);
            }

            //Console.WriteLine($"UeVar: [{string.Join(", ", livenessAnalysis.UeVar[block])}]");
            //Console.WriteLine($"VarNotKilled: [{string.Join(", ", livenessAnalysis.VarNotKilled[block])}]");

            //Console.WriteLine($"LiveIn: [{string.Join(", ", livenessAnalysis.LiveIn[block])}]");
            Console.WriteLine($"LiveOut: [{string.Join(", ", livenessAnalysis.LiveOut[block])}]");
        }

        Console.WriteLine("");
        foreach (var range in livenessRangeAnalysis.GetRanges())
        {
            Console.WriteLine(
                $"Range: {range.Representent} = [{string.Join(", ", range.Set)}], color = {coloring[range.Representent]}");
        }

        Console.WriteLine("");
        foreach (var conflict in livenessRangeInterferenceAnalysis.LivenessRangeInterferencesGraph.Edges)
        {
            if (conflict.Item1 != conflict.Item2)
            {
                Console.WriteLine($"Range interference: {conflict.Item1} - {conflict.Item2}");
            }
        }

        var codeGen = new MacroBCodeGenerator(tacControlFlowGraph);
        Console.WriteLine(codeGen.GenerateCode());
    }

    private static string GetBlockName(TacInstructionBlock block)
    {
        return $"Block #{block.Id} at offset {block.EntryOffset}";
    }
}