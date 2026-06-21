using IlMacroB.Host.FrontEnd;

using Rollomatic.IlMacroB.FrontEnd;

namespace Rollomatic.IlMacroB.Demo;

// ToDo: 1. Merge consecutive control flow blocks
//       2. Rebuild mathematical expressions
//       3. Whole program code generation (fallthrough simplification, etc, see 1.)
//       4. Detect and reconstruct IF-block
//       5. Initialize the procedure arguments
//       6. Implement all mathematical operators,
//       7. Sketch an interface to the CNC
//       8. Logging?
//       9. Debugging?
//       10. Synchronous communication chanel
//       11. Read/Write ordering?
//       12. Simulation/Unit testing?

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
        var device = new ConsoleDumpDevice();

        device.Dispatch(IsoProgramDemo, 1, 3);

        return 0;
    }
}
