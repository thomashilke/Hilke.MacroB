namespace IlMacroB.Host.FrontEnd;

public static class Cnc
{
    /// <summary>Characteristics of the runtime environment.</summary>
    /// <remarks>
    ///   Source: FANUC Series 30i/300i/300is-MODEL A Series 31i/310i/310is-MODEL A Series 32i/320i/320is-MODEL A User's Manual, ref: B-63944EN/03
    /// </remarks>
    public static class RuntimeEnvironment
    {
        public const int MaxCallDepth = 15;
        public const int MaxMacroCallDepth = 5;
        public const int MaxSubprogramCallDepth = 10;

        public static readonly Dictionary<char, int> ArgumentSpecificationI = new()
        {
            {'A', 1},
            {'B', 2},
            {'C', 3},
            {'I', 4},
            {'J', 5},
            {'K', 6},
            {'D', 7},
            {'E', 8},
            {'F', 9},
            {'H', 11},
            {'M', 13},
            {'Q', 17},
            {'R', 18},
            {'S', 19},
            {'T', 20},
            {'U', 21},
            {'V', 22},
            {'W', 23},
            {'X', 24},
            {'Y', 25},
            {'Z', 26},
        };

        public static readonly MacroVariableRange LocalVariableRange = new MacroVariableRange(1, 33);

        public static readonly IReadOnlyList<MacroVariableRange> CommonVariableRanges =
            new List<MacroVariableRange>()
            {
                new MacroVariableRange(100, 199),
                new MacroVariableRange(500, 999)
            };

        public sealed class MacroVariableRange
        {
            public MacroVariableRange(int lowerBound, int upperBound)
            {
                if (lowerBound < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(lowerBound), lowerBound, "");
                }

                if (upperBound < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(upperBound), upperBound, "");
                }

                LowerBound = lowerBound;
                UpperBound = upperBound;
            }

            /// <summary>The inclusive lower bound of the range.</summary>
            public int LowerBound { get; }

            /// <summary>The inclusize upper bound of the range.</summary>
            public int UpperBound { get; }
        }
    }

    public interface IDevice
    {
        void Dispatch(Action action);
        void Dispatch<T1>(Action<T1> action, T1 arg1);
        void Dispatch<T1, T2>(Action<T1, T2> action, T1 arg1, T2 arg2);
        void Dispatch<T1, T2, T3>(Action<T1, T2, T3> action, T1 arg1, T2 arg2, T3 arg3);
    }

    private static void ThrowSupportedOnlyOnCnc() =>
        throw new NotSupportedException("Only supported on the Cnc.");

    public static void RaiseAlarm(int alarm, string message) => ThrowSupportedOnlyOnCnc();

    public static void Stop(string message) => ThrowSupportedOnlyOnCnc();

    public static class Machine
    {
        public static void Move(
            double x, double y, double z,
            double feed
            /*double? x = null, double? y = null, double? z = null,
              double? a = null, double? b = null, double? c = null*/) =>
            ThrowSupportedOnlyOnCnc();

        public static void FastMove(
            double? x = null, double? y = null, double? z = null,
            double? a = null, double? b = null, double? c = null) =>
            ThrowSupportedOnlyOnCnc();


        public static void StartCoolant() => ThrowSupportedOnlyOnCnc();

        public static void StopCoolant() => ThrowSupportedOnlyOnCnc();

        public static class State
        {
            public static double Clock1 => throw new NotSupportedException();
            public static double Clock2 => throw new NotSupportedException();
        }
    }

    public static class Math
    {
        public static double Sin(double x) => throw new NotSupportedException();
        public static double Cos(double x) => throw new NotSupportedException();
        public static double Tan(double x) => throw new NotSupportedException();
        public static double ASin(double x) => throw new NotSupportedException();
        public static double ACos(double x) => throw new NotSupportedException();
        public static double ATan(double x) => throw new NotSupportedException();
        public static double ATan(double x, double y) => throw new NotSupportedException();
        public static double Sqrt(double x) => throw new NotSupportedException();
        public static double Abs(double x) => throw new NotSupportedException();
        public static double Bin(double x) => throw new NotSupportedException();
        public static double Bcd(double x) => throw new NotSupportedException();
        public static double Round(double x) => throw new NotSupportedException();
        public static double Fix(double x) => throw new NotSupportedException();
        public static double Fup(double x) => throw new NotSupportedException();
        public static double Log(double x) => throw new NotSupportedException();
        public static double Exp(double x) => throw new NotSupportedException();
        public static double Pow(double x, double y) => throw new NotSupportedException();
    }
}
