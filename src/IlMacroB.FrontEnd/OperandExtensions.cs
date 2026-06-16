namespace Rollomatic.IlMacroB.FrontEnd;

public static class OperandExtensions
{
    public static bool IsControlFlow(this Operand operand)
    {
        return operand switch
        {
            Operand.BrTrue => true,
            Operand.BrFalse => true,
            Operand.Ble => true,
            Operand.Blt => true,
            Operand.Bge => true,
            Operand.Bgt => true,
            Operand.Beq => true,
            Operand.Bne => true,
            Operand.Br => true,
            Operand.Ret => true,
            _ => false
        };
    }
}