using System.ComponentModel;

using Hilke.MacroB.FrontEnd;

namespace MacroB.Host.FrontEnd;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class MacroCallConventionAttribute : Attribute
{
    public MacroCallConventionAttribute(MacroCallConvention callConvention)
    {
        if (!Enum.IsDefined(callConvention))
        {
            throw new InvalidEnumArgumentException(
                nameof(callConvention),
                (int)callConvention,
                typeof(MacroCallConvention));
        }

        CallConvention = callConvention;
    }

    public MacroCallConvention CallConvention { get; }
}
