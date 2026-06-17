namespace Rollomatic.IlMacroB.FrontEnd;

public interface IControlFlowGraphTransformation<TBlock> where TBlock : IAdjacencyVertex<TBlock>
{
    void Transform(ControlFlowGraph<TBlock> controlFlowGraph);
}