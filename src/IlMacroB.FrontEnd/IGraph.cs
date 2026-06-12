namespace Rollomatic.IlMacroB.FrontEnd;

public interface IGraph<TVertex>
{
    IEnumerable<TVertex> GetSuccessors(TVertex vertex);

    IEnumerable<TVertex> GetPredecessors(TVertex vertex);
}
