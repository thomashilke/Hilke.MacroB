using IlMacroB.Host.FrontEnd;

namespace Demo;

public class Program
{
    public static void IsoProgramDemo(int a, int b)
    {
        if (a > 5)
        {
            a -= 5;
        }

        for (var i = 0; i < 3; ++i)
        {
            a += i;
        }

        a *= 2;
        Cnc.Machine.StartCoolant();
        Cnc.Machine.Move(
            feed: 500.0,
            x: Cnc.Math.Sin(a + b),
            y: 2.0,
            z: 34.0);
        Cnc.Machine.StopCoolant();
    }

    public static int Main(string[] args)
    {
        var device = new StringDevice();
        
        device.Dispatch(IsoProgramDemo, 1, 3);
        Console.WriteLine(device.Code);

        return 0;
    }
}
