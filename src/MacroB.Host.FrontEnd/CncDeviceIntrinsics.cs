using System.Reflection;

using Hilke.MacroB.FrontEnd;

namespace MacroB.Host.FrontEnd;

public static class CncDeviceIntrinsics
{
    public static IReadOnlySet<MethodInfo> Methods { get; } = new HashSet<MethodInfo>
    {
        typeof(Cnc.Machine).GetMethod(nameof(Cnc.Machine.Move))!,
        typeof(Cnc.Machine).GetMethod(nameof(Cnc.Machine.FastMove))!,
        typeof(Cnc.Machine).GetMethod(nameof(Cnc.Machine.StartCoolant))!,
        typeof(Cnc.Machine).GetMethod(nameof(Cnc.Machine.StopCoolant))!,
        typeof(Cnc).GetMethod(nameof(Cnc.RaiseAlarm))!,
        typeof(Cnc).GetMethod(nameof(Cnc.Stop))!,
    };

    // ISO block rendering templates for the device intrinsics MacroBCodeGenerator.RenderNcStatement
    // knows how to emit (a subset of Methods above — RaiseAlarm/Stop/FastMove are not yet lowered to
    // an NC statement). Keyed by MethodInfo.Name to match IntrinsicFunctionCall.FunctionName, the
    // only identifier that survives IL parsing/TAC lowering into MacroBCodeGenerator. Mirrors
    // CncMathIntrinsics.Map: the mapping from a Cnc DSL method to its Macro B rendering lives here in
    // the front-end, not hardcoded inside the code generator.
    public static IReadOnlyDictionary<string, NcStatementTemplate> Map { get; } = BuildMap();

    private static IReadOnlyDictionary<string, NcStatementTemplate> BuildMap()
    {
        return new Dictionary<string, NcStatementTemplate>
        {
            [nameof(Cnc.Machine.Move)] = new NcStatementTemplate("G01", new[] { "X", "Y", "Z", "F" }),
            [nameof(Cnc.Machine.StartCoolant)] = new NcStatementTemplate("M35", Array.Empty<string>()),
            [nameof(Cnc.Machine.StopCoolant)] = new NcStatementTemplate("M39", Array.Empty<string>()),
        };
    }
}
