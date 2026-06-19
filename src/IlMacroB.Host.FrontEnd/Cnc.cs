namespace IlMacroB.Host.FrontEnd;

public static class Cnc
{
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
            double feed,
            double x, double y, double z
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