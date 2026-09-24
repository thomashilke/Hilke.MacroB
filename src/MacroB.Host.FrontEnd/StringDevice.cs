using System.Reflection;
using System.Text;

using Hilke.MacroB.FrontEnd;

namespace MacroB.Host.FrontEnd;

public sealed class StringDevice : Cnc.IDevice
{
    private readonly MacroVariableConfiguration _configuration;

    public StringDevice(MacroVariableConfiguration? configuration = null)
    {
        _configuration = configuration ?? MacroVariableConfiguration.Default;
    }

    private MacroBCodeGenerator BuildCodeGenerator(
        MethodInfo method,
        IReadOnlyDictionary<MethodInfo, int> procedureNumbers,
        IReadOnlyDictionary<MethodInfo, MacroCallConvention> conventions,
        int registerBase,
        int? registerCount)
    {
        var instructions = IlParser.ParseMethod(method);
        var tacControlFlowGraph = TacConverter.Convert(
            ControlFlowGraphBuilder.Create(instructions), CncMathIntrinsics.Map, procedureNumbers, conventions);

        var renameTransform = new StaticSingleAssignmentRenameTransform();
        tacControlFlowGraph = renameTransform.Transform(tacControlFlowGraph);

        var constantFoldingTransform = new ConstantPropagatorTransform();
        tacControlFlowGraph = constantFoldingTransform.Transform(tacControlFlowGraph);

        var dceTransform = new DeadCodeEliminationTransform();
        tacControlFlowGraph = dceTransform.Transform(tacControlFlowGraph);

        var basicBlockMergeTransform = new BasicBlockMergeTransform();
        tacControlFlowGraph = basicBlockMergeTransform.Transform(tacControlFlowGraph);

        var expressionRebuildTransform = new ExpressionRebuildTransform();
        tacControlFlowGraph = expressionRebuildTransform.Transform(tacControlFlowGraph);

        var returnValueSlot = method.ReturnType == typeof(void) ? (int?)null : _configuration.ReturnValueVariable;
        return new MacroBCodeGenerator(tacControlFlowGraph, _configuration, CncDeviceIntrinsics.Map, registerBase, registerCount, returnValueSlot);
    }

    private string BuildProgram(MethodInfo method, object[] arguments)
    {
        var (procedureNumbers, conventions) = ProcedureDiscovery.Discover(method, CncMathIntrinsics.Map, _configuration);
        var sb = new StringBuilder(
            BuildCodeGenerator(method, procedureNumbers, conventions, _configuration.GeneralPurposeRegisterBase, null)
               .GenerateCode(arguments));

        foreach (var (procedureMethod, programNumber) in procedureNumbers.OrderBy(kvp => kvp.Value))
        {
            var parameterCount = procedureMethod.GetParameters().Length;
            var convention = conventions[procedureMethod];
            var argumentCap = convention == MacroCallConvention.MacroCallStyle1 ? MacroCallStyle1Arguments.MaxArguments : 100;

            if (parameterCount > argumentCap)
            {
                throw new NotSupportedException(
                    $"{procedureMethod} has too many parameters for {convention} (max {argumentCap}).");
            }

            var (registerBase, registerCount) = convention == MacroCallConvention.SubProgramCall
                ? (_configuration.SubProgramRegisterBase + (programNumber - _configuration.FirstProgramNumber) * _configuration.SubProgramRegisterStride,
                   (int?)_configuration.SubProgramRegisterStride)
                : (_configuration.GeneralPurposeRegisterBase, (int?)_configuration.GeneralPurposeRegisterCount);

            sb.AppendLine();
            sb.Append(
                BuildCodeGenerator(procedureMethod, procedureNumbers, conventions, registerBase, registerCount)
                   .GenerateProcedureCode(programNumber, parameterCount, convention));
        }

        return sb.ToString();
    }

    public void Dispatch(Action action)
    {
        if (action.Method.GetParameters().Count() != 0)
        {
            throw new ArgumentException("Wrong number of parameters.");
        }

        Code = BuildProgram(action.Method, Array.Empty<object>());
    }

    public string Code { get; private set; } = String.Empty;

    public void Dispatch<T1>(Action<T1> action, T1 arg1)
    {
        if (action.Method.GetParameters().Count() != 1)
        {
            throw new ArgumentException("Wrong number of parameters.");
        }

        Code = BuildProgram(action.Method, new object[] { arg1! });
    }

    public void Dispatch<T1, T2>(Action<T1, T2> action, T1 arg1, T2 arg2)
    {
        if (action.Method.GetParameters().Count() != 2)
        {
            throw new ArgumentException("Wrong number of parameters.");
        }

        Code = BuildProgram(action.Method, new object[] { arg1!, arg2! });
    }

    public void Dispatch<T1, T2, T3>(Action<T1, T2, T3> action, T1 arg1, T2 arg2, T3 arg3)
    {
        if (action.Method.GetParameters().Count() != 3)
        {
            throw new ArgumentException("Wrong number of parameters.");
        }

        Code = BuildProgram(action.Method, new object[] { arg1!, arg2!, arg3! });
    }
}
