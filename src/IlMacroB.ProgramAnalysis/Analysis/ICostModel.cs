public interface ICostModel<in TVertex>
{
    // This is a very naive naive class of cost model.
    // In practice, the cost of jumping from source to target may depends:
    //  - the order of source and target is the program file (jumping forward may be cheaper than jumping backward)
    //  - the total size of the program
    //  - the distance between source and target in the program
    //  - the recent execution trace, due to caching of recently seen jump target, or recently target jumped to,
    //  - the actual configuration of the CNC.
    // The list is not exhaustive
    double GetTransitionCost(TVertex source, TVertex target);
}
