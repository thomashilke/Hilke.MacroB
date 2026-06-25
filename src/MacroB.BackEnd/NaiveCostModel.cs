using Hilke.MacroB.FrontEnd;

namespace Hilke.MacroB;

public class NaiveCostModel : ICostModel<TacInstructionBlock>
{
    /// <inheritdoc />
    public double GetTransitionCost(TacInstructionBlock source, TacInstructionBlock target)
    {
        return 1.0;
    }
}
