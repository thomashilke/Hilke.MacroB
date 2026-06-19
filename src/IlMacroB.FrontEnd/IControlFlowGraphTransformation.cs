namespace Rollomatic.IlMacroB.FrontEnd;

public interface IControlFlowGraphTransformation<TBlock> where TBlock : IAdjacencyVertex<TBlock>
{
    ControlFlowGraph<TBlock> Transform(ControlFlowGraph<TBlock> controlFlowGraph);
}