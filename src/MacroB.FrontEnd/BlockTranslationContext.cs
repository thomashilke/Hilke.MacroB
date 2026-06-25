namespace Hilke.MacroB.FrontEnd;

public class BlockTranslationContext
{
    public List<OperandBase>? IncomingStack { get; internal set; }

    public List<OperandBase>? OutgoingStack { get; internal set; }
}
