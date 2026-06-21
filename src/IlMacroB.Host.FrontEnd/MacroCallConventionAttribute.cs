using System.ComponentModel;

namespace IlMacroB.Host.FrontEnd;

public sealed class MacroCallConventionAttribute
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
