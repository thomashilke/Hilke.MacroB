using System.Reflection;
using System.Reflection.Emit;

namespace Rollomatic.IlMacroB.FrontEnd;

public static class IlParser
{
    private static readonly Dictionary<short, OpCode> OpCodeMap = new();

    static IlParser()
    {
        // Cache all standard .NET IL opcodes for O(1) runtime lookup
        foreach (var field in typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (field.FieldType == typeof(OpCode))
            {
                var op = (OpCode)field.GetValue(null);
                OpCodeMap[op.Value] = op;
            }
        }
    }

    public static List<IlInstruction> ParseMethod(MethodInfo method)
    {
        var body = method.GetMethodBody();
        if (body is null)
        {
            return new List<IlInstruction>();
        }

        var il = body.GetILAsByteArray();
        var module = method.Module;
        var instructions = new List<IlInstruction>();
        var position = 0;

        while (position < il.Length)
        {
            var startOffset = position;
            var opValue = (short)il[position++];

            // Handle two-byte opcodes (prefixes like 0xFE)
            if (opValue == 0xFE && position < il.Length)
            {
                opValue = (short)((opValue << 8) | il[position++]);
            }

            if (!OpCodeMap.TryGetValue(opValue, out var opCode))
            {
                throw new InvalidOperationException($"Unknown opcode: 0x{opValue:X}");
            }

            // Read the varying-length operand based on its metadata type
            var operand = ReadOperand(il, ref position, opCode.OperandType, module);

            instructions.Add(
                new IlInstruction(
                    startOffset,
                    opCode,
                    position - startOffset,
                    operand));
        }

        CalculateAbsoluteTargets(instructions);

        return instructions;
    }

    private static void CalculateAbsoluteTargets(List<IlInstruction> instructions)
    {
        foreach (var inst in instructions)
        {
            if (inst.OpCode.OperandType == OperandType.ShortInlineBrTarget && inst.Operand is int relShort)
            {
                inst.AbsoluteTarget = inst.NextInstructionOffset + relShort;
            }
            else if (inst.OpCode.OperandType == OperandType.InlineBrTarget && inst.Operand is int relLong)
            {
                inst.AbsoluteTarget = inst.NextInstructionOffset + relLong;
            }
        }
    }

    private static object? ReadOperand(byte[] il, ref int pos, OperandType type, Module module)
    {
        switch (type)
        {
            // No operand
            case OperandType.InlineNone:
                return null;

            // 1-byte operands
            case OperandType.ShortInlineBrTarget:
                return (int)(sbyte)il[pos++]; // Relative branch target offset
            case OperandType.ShortInlineI:
                return (int)(sbyte)il[pos++]; // sbyte literal
            case OperandType.ShortInlineVar:
                return il[pos++]; // Local variable index

            // 2-byte operands
            case OperandType.InlineVar:
                var varIndex = BitConverter.ToInt16(il, pos);
                pos += 2;
                return varIndex;

            // 4-byte numeric or control flow targets
            case OperandType.InlineBrTarget:
                var brTarget = BitConverter.ToInt32(il, pos);
                pos += 4;
                return brTarget;
            case OperandType.InlineI:
                var intVal = BitConverter.ToInt32(il, pos);
                pos += 4;
                return intVal;
            case OperandType.ShortInlineR:
                var floatVal = BitConverter.ToSingle(il, pos);
                pos += 4;
                return floatVal;

            // 8-byte numeric literals
            case OperandType.InlineI8:
                var longVal = BitConverter.ToInt64(il, pos);
                pos += 8;
                return longVal;
            case OperandType.InlineR:
                var doubleVal = BitConverter.ToDouble(il, pos);
                pos += 8;
                return doubleVal;

            // Metadata tokens (Methods, Fields, Types, Strings)
            case OperandType.InlineField:
            case OperandType.InlineMethod:
            case OperandType.InlineType:
            case OperandType.InlineTok:
                var token = BitConverter.ToInt32(il, pos);
                pos += 4;
                return ResolveToken(module, token);

            case OperandType.InlineString:
                var stringToken = BitConverter.ToInt32(il, pos);
                pos += 4;
                return module.ResolveString(stringToken);

            // Switch table (Varying size)
            case OperandType.InlineSwitch:
                var count = BitConverter.ToInt32(il, pos);
                pos += 4;
                var targets = new int[count];
                for (var i = 0; i < count; i++)
                {
                    targets[i] = BitConverter.ToInt32(il, pos);
                    pos += 4;
                }

                return targets;

            default:
                throw new NotSupportedException($"Unsupported operand type: {type}");
        }
    }

    private static object ResolveToken(Module module, int token)
    {
        try
        {
            // Dynamically resolve member pointers from the hosting module context
            return module.ResolveMember(token);
        }
        catch
        {
            return $"Token: 0x{token:X8}";
        }
    }
}