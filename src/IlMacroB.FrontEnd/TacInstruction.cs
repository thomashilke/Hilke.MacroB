namespace Rollomatic.IlMacroB.FrontEnd;

public class TacInstruction
{
    public TacInstruction(string destination, Operand op, params object[] arguments)
    {
        Destination = new SsaVariable(destination);
        Op = op;
        Arguments = arguments.ToList();
    }

    public TacInstruction(Operand op, params object[] arguments)
    {
        Op = op;
        Arguments = arguments.ToList();
    }

    public SsaVariable? Destination { get; set; }

    public Operand Op { get; }

    public List<object> Arguments { get; set; }

    public bool IsBranch => Op.IsControlFlow();

    public override string ToString()
    {
        switch (Op)
        {
            case Operand.Assign:
                return $"{Destination} = {Arguments.Single()}";

            case Operand.Ret:
                return $"Return {Arguments.Single()}";
            case Operand.Br:

                return $"Goto {Arguments.Single()}";
            case Operand.BrTrue:
                return $"Goto {Arguments[1]} if {Arguments[0]} else goto {Arguments[2]}";
            case Operand.BrFalse:
                return $"Goto {Arguments[1]} if not {Arguments[0]} else goto {Arguments[2]}";

            case Operand.Beq:
                return $"Goto {Arguments[2]} if {Arguments[0]} == {Arguments[1]} else goto {Arguments[3]}";
            case Operand.Bne:
                return $"Goto {Arguments[2]} if {Arguments[0]} != {Arguments[1]} else goto {Arguments[3]}";
            case Operand.Blt:
                return $"Goto {Arguments[2]} if {Arguments[0]} < {Arguments[1]} else goto {Arguments[3]}";
            case Operand.Ble:
                return $"Goto {Arguments[2]} if {Arguments[0]} <= {Arguments[1]} else goto {Arguments[3]}";
            case Operand.Bgt:
                return $"Goto {Arguments[2]} if {Arguments[0]} > {Arguments[1]} else goto {Arguments[3]}";
            case Operand.Bge:
                return $"Goto {Arguments[2]} if {Arguments[0]} >= {Arguments[1]} else goto {Arguments[3]}";

            case Operand.Call:
                return Destination is null
                           ? $"{Arguments.First()}({string.Join(", ", Arguments.Skip(1))})"
                           : $"{Destination} = {Arguments.First()}({string.Join(", ", Arguments.Last())})";

            case Operand.Phi:
                return $"{Destination} = Phi({string.Join(", ", Arguments)})";
            case Operand.Sin:
                return $"{Destination} = {Arguments[0]} {Op}";
            case Operand.Add:
            case Operand.Sub:
            case Operand.Mul:
            case Operand.Div:
            case Operand.Cgt:
                return $"{Destination} = {Arguments[0]} {Op} {Arguments[1]}";
            default:
                throw new NotSupportedException();
        }
    }
}
