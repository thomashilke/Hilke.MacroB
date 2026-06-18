using Rollomatic.IlMacroB.FrontEnd;

namespace Rollomatic.IlMacroB.Samples.DumpIl;

public static class Cnc
{
    private static void ThrowSupportedOnlyOnCnc() =>
        throw new NotSupportedException("Only supported on the Cnc.");

    public static void RaiseAlarm(int alarm, string message) => ThrowSupportedOnlyOnCnc();

    public static void Stop(string message) => ThrowSupportedOnlyOnCnc();

    public static class Machine
    {
        public static void Move(
            double feed,
            double x, double y, double z
            /*double? x = null, double? y = null, double? z = null,
              double? a = null, double? b = null, double? c = null*/) =>
            ThrowSupportedOnlyOnCnc();

        public static void FastMove(
            double? x = null, double? y = null, double? z = null,
            double? a = null, double? b = null, double? c = null) =>
            ThrowSupportedOnlyOnCnc();


        public static void StartCoolant() => ThrowSupportedOnlyOnCnc();

        public static void StopCoolant() => ThrowSupportedOnlyOnCnc();

        public static class State
        {
            public static double Clock1 => throw new NotSupportedException();
            public static double Clock2 => throw new NotSupportedException();

        }
    }

    public static class Math
    {
        public static double Sin(double x) => throw new NotSupportedException();
        public static double Cos(double x) => throw new NotSupportedException();
        public static double Tan(double x) => throw new NotSupportedException();
        public static double ASin(double x) => throw new NotSupportedException();
        public static double ACos(double x) => throw new NotSupportedException();
        public static double ATan(double x) => throw new NotSupportedException();
        public static double ATan(double x, double y) => throw new NotSupportedException();
        public static double Sqrt(double x) => throw new NotSupportedException();
        public static double Abs(double x) => throw new NotSupportedException();
        public static double Bin(double x) => throw new NotSupportedException();
        public static double Bcd(double x) => throw new NotSupportedException();
        public static double Round(double x) => throw new NotSupportedException();
        public static double Fix(double x) => throw new NotSupportedException();
        public static double Fup(double x) => throw new NotSupportedException();
        public static double Log(double x) => throw new NotSupportedException();
        public static double Exp(double x) => throw new NotSupportedException();
        public static double Pow(double x, double y) => throw new NotSupportedException();
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

    public static double AddTwoNumbers(int a, int b)
    {
        if (a > 5)
        {
            a -= 5;
        }

        a *= 2;

        Cnc.Machine.StartCoolant();

        Cnc.Machine.Move(500.0, 5.0, 2.0, 34.0);

        return Cnc.Math.Sin(a + b);
    }


    // ToDo: 1. Merge consecutive control flow blocks
    //       2. Rebuild mathematical expressions
    //       3. Whole program code generation (fallthrough simplification, etc, see 1.)
    //       4. Detect and reconstruct IF-block
    //       5. Initialize the procedure arguments
    //       6. Implement all mathematical operators,
    //       7. Sketch an interface to the CNC
    //       8. Logging?
    //       9. Debugging?
    //       10. Synchronous communication chanel
    //       11. Read/Write ordering?
    //       12. Simulation/Unit testing?

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
            Console.WriteLine($"Range: {range.Representent} = [{string.Join(", ", range.Set)}], color = {coloring[range.Representent]}");
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
        foreach (var block in blockOrderAnalysis.BlockOrder)
        {
            Console.WriteLine(codeGen.GenerateCode(block));
        }

        return 0;
    }

    private static string GetBlockName(TacInstructionBlock block)
    {
        return $"Block #{block.Id} at offset {block.EntryOffset}";
    }
}
