using System.Reflection;

using Rollomatic.IlMacroB.FrontEnd;

namespace IlMacroB.Host.FrontEnd;

public sealed class StringDevice : Cnc.IDevice
{
    private MacroBCodeGenerator BuildCodeGenerator(MethodInfo method)
    {
        var instructions = IlParser.ParseMethod(method);
        var tacControlFlowGraph = TacConverter.Convert(
            ControlFlowGraphBuilder.Create(instructions));

        var renameTransform = new StaticSingleAssignmentRenameTransform();
        tacControlFlowGraph = renameTransform.Transform(tacControlFlowGraph);

        var constantFoldingTransform = new ConstantPropagatorTransform();
        tacControlFlowGraph = constantFoldingTransform.Transform(tacControlFlowGraph);

        var dceTransform = new DeadCodeEliminationTransform();
        tacControlFlowGraph = dceTransform.Transform(tacControlFlowGraph);

        var basicBlockMergeTransform = new BasicBlockMergeTransform();
        tacControlFlowGraph = basicBlockMergeTransform.Transform(tacControlFlowGraph);

        var codeGenerator = new MacroBCodeGenerator(tacControlFlowGraph);

        return codeGenerator;
    }

    public void Dispatch(Action action)
    {
        if (action.Method.GetParameters().Count() != 0)
        {
            throw new ArgumentException("Wrong number of parameters.");
        }

        var codeGenerator = BuildCodeGenerator(action.Method);
        Code = codeGenerator.GenerateCode();
    }

    public string Code { get; private set; } = String.Empty;

    public void Dispatch<T1>(Action<T1> action, T1 arg1)
    {
        if (action.Method.GetParameters().Count() != 1)
        {
            throw new ArgumentException("Wrong number of parameters.");
        }

        var codeGenerator = BuildCodeGenerator(action.Method);
        Code = codeGenerator.GenerateCode(arg1);
    }

    public void Dispatch<T1, T2>(Action<T1, T2> action, T1 arg1, T2 arg2)
    {
        if (action.Method.GetParameters().Count() != 2)
        {
            throw new ArgumentException("Wrong number of parameters.");
        }

        var codeGenerator = BuildCodeGenerator(action.Method);
        Code = codeGenerator.GenerateCode(arg1, arg2);
    }

    public void Dispatch<T1, T2, T3>(Action<T1, T2, T3> action, T1 arg1, T2 arg2, T3 arg3)
    {
        if (action.Method.GetParameters().Count() != 3)
        {
            throw new ArgumentException("Wrong number of parameters.");
        }

        var codeGenerator = BuildCodeGenerator(action.Method);
        Code = codeGenerator.GenerateCode(arg1, arg2, arg3);
    }
}