using System.Reflection.Emit;

namespace Rollomatic.IlMacroB.FrontEnd;

public class IlInstruction
{
    public IlInstruction(int offset, OpCode opCode, int length, object? operand)
    {
        Offset = offset;
        OpCode = opCode;
        Operand = operand;
        Length = length;
    }


    public int Offset { get; }

    public int Length { get; }

    public OpCode OpCode { get; }

    public object? Operand { get; }

    public int? AbsoluteTarget { get; internal set; }

    public int NextInstructionOffset => Offset + Length;

    public override string ToString() => $"{Offset:X4}: {OpCode} {Operand}";
}
