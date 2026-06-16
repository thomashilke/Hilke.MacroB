namespace Rollomatic.IlMacroB.FrontEnd;

public class BlockTranslationContext
{
    public List<string>? IncomingStack { get; internal set; }

    public List<string>? OutgoingStack { get; internal set; }
}