using System.Reflection;

using Hilke.MacroB.FrontEnd;

namespace MacroB.Host.FrontEnd;

public static class CncMathIntrinsics
{
    public static IReadOnlyDictionary<MethodInfo, Operand> Map { get; } = BuildMap();

    private static IReadOnlyDictionary<MethodInfo, Operand> BuildMap()
    {
        MethodInfo M(string name, params Type[] parameterTypes) =>
            typeof(Cnc.Math).GetMethod(name, parameterTypes)
            ?? throw new MissingMethodException(typeof(Cnc.Math).FullName, name);

        return new Dictionary<MethodInfo, Operand>
        {
            [M(nameof(Cnc.Math.Sin), typeof(double))] = Operand.Sin,
            [M(nameof(Cnc.Math.Cos), typeof(double))] = Operand.Cos,
            [M(nameof(Cnc.Math.Tan), typeof(double))] = Operand.Tan,
            [M(nameof(Cnc.Math.ASin), typeof(double))] = Operand.ASin,
            [M(nameof(Cnc.Math.ACos), typeof(double))] = Operand.ACos,
            [M(nameof(Cnc.Math.ATan), typeof(double))] = Operand.ATan,
            [M(nameof(Cnc.Math.ATan), typeof(double), typeof(double))] = Operand.ATan2,
            [M(nameof(Cnc.Math.Sqrt), typeof(double))] = Operand.Sqrt,
            [M(nameof(Cnc.Math.Abs), typeof(double))] = Operand.Abs,
            [M(nameof(Cnc.Math.Bin), typeof(double))] = Operand.Bin,
            [M(nameof(Cnc.Math.Bcd), typeof(double))] = Operand.Bcd,
            [M(nameof(Cnc.Math.Round), typeof(double))] = Operand.Round,
            [M(nameof(Cnc.Math.Fix), typeof(double))] = Operand.Fix,
            [M(nameof(Cnc.Math.Fup), typeof(double))] = Operand.Fup,
            [M(nameof(Cnc.Math.Log), typeof(double))] = Operand.Ln,
            [M(nameof(Cnc.Math.Exp), typeof(double))] = Operand.Exp,
            [M(nameof(Cnc.Math.Pow), typeof(double), typeof(double))] = Operand.Pow,
        };
    }
}
