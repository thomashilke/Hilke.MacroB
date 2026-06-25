using DiffEngine;
using MacroB.Host.FrontEnd;

public class StringDeviceTests
{
    [SetUp]
    public void SetUp()
    {
        DiffRunner.Disabled = true;
    }

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

    [Test]
    public Task Given_a_program_calling_dispatch_device_should_generate_valid_macrob_program()
    {
        var device = new StringDevice();

        device.Dispatch(IsoProgramDemo, 1, 3);

        return Verify(device.Code);
    }
}
