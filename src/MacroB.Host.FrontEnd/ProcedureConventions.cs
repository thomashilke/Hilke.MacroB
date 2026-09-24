using System.Reflection;

using Hilke.MacroB.FrontEnd;

namespace MacroB.Host.FrontEnd;

internal static class ProcedureConventions
{
    public const MacroCallConvention Default = MacroCallConvention.SubProgramCall;

    public static MacroCallConvention Resolve(MethodInfo method)
    {
        var attribute = method.GetCustomAttribute<MacroCallConventionAttribute>();

        if (attribute is null)
        {
            return Default;
        }

        return attribute.CallConvention switch
        {
            MacroCallConvention.MacroCall => MacroCallConvention.MacroCallStyle1,
            MacroCallConvention.SubProgramCall => MacroCallConvention.SubProgramCall,
            MacroCallConvention.MacroCallStyle1 => MacroCallConvention.MacroCallStyle1,
            _ => throw new NotSupportedException(
                $"{method}: MacroCallConvention.{attribute.CallConvention} is not implemented; " +
                "use MacroCallStyle1, MacroCall, or SubProgramCall (or leave the attribute off for the default).")
        };
    }
}
