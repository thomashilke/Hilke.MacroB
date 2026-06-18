using Rollomatic.IlMacroB.FrontEnd;

namespace Rollomatic.IlMacroB;

public class NaiveCostModel : ICostModel<TacInstructionBlock>
{
    /// <inheritdoc />
    public double GetTransitionCost(TacInstructionBlock source, TacInstructionBlock target)
    {
        return 1.0;
    }
}