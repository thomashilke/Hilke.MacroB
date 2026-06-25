using IlMacroB.Host.FrontEnd;

namespace Demo;

public class Program
{
    public static void IsoProgramDemo(int a, int b)
    {
        var dAlpha = Cnc.Math.Sin(Math.PI / 3.0);
        if (a > 5)
        {
            a -= 5;
        }

        a *= 2;

        Cnc.Machine.StartCoolant();

        var position = 0.0;
        for (var i = 1; i < 3; ++i)
        {
            Cnc.Machine.Move(
                feed: 500.0,
                x: Cnc.Math.Sin(a + b),
                y: position,
                z: 34.0);

            position += dAlpha;
        }

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
