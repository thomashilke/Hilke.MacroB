using DiffEngine;
using Hilke.MacroB.FrontEnd;
using MacroB.Host.FrontEnd;

namespace MacroB.ProgramAnalysis.Tests;

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
            x: Cnc.Math.Sin((a + b) * 2),
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

    public static void MathIntrinsicDemo(double a)
    {
        Cnc.Machine.Move(
            feed: 100.0,
            x: Cnc.Math.Pow(a, 2.0),
            y: 0.0,
            z: 0.0);
    }

    [Test]
    public Task Given_a_program_calling_a_non_sin_math_intrinsic_should_compile()
    {
        var device = new StringDevice();

        device.Dispatch(MathIntrinsicDemo, 3.0);

        return Verify(device.Code);
    }

    private static void MoveTo(double x, double y)
    {
        Cnc.Machine.Move(feed: 100.0, x: x, y: y, z: 0.0);
    }

    private static double Average(double a, double b) => (a + b) / 2.0;

    public static void SubProgramCallDemo(double x, double y)
    {
        MoveTo(x, y);
        var avg = Average(x, y);
        Cnc.Machine.Move(feed: 50.0, x: avg, y: 0.0, z: 0.0);
    }

    [Test]
    public Task Given_a_program_calling_unattributed_helpers_should_use_subprogram_call_convention()
    {
        var device = new StringDevice();

        device.Dispatch(SubProgramCallDemo, 2.0, 4.0);

        return Verify(device.Code);
    }

    [MacroCallConvention(MacroCallConvention.MacroCallStyle1)]
    private static void MacroMoveTo(double x, double y)
    {
        Cnc.Machine.Move(feed: 100.0, x: x, y: y, z: 0.0);
    }

    [MacroCallConvention(MacroCallConvention.MacroCallStyle1)]
    private static double MacroAverage(double a, double b) => (a + b) / 2.0;

    public static void MacroCallDemo(double x, double y)
    {
        MacroMoveTo(x, y);
        var avg = MacroAverage(x, y);
        Cnc.Machine.Move(feed: 50.0, x: avg, y: 0.0, z: 0.0);
    }

    [Test]
    public Task Given_a_program_calling_macrostyle1_attributed_helpers_should_use_g65_letter_arguments()
    {
        var device = new StringDevice();

        device.Dispatch(MacroCallDemo, 2.0, 4.0);

        return Verify(device.Code);
    }

    [MacroCallConvention(MacroCallConvention.Inline)]
    private static void InlineHelper()
    {
        Cnc.Machine.StartCoolant();
    }

    public static void InlineRejectionDemo()
    {
        InlineHelper();
    }

    [MacroCallConvention(MacroCallConvention.MacroCallStyle2)]
    private static void Style2Helper()
    {
        Cnc.Machine.StopCoolant();
    }

    public static void Style2RejectionDemo()
    {
        Style2Helper();
    }

    [Test]
    public void Given_a_program_calling_an_inline_convention_procedure_should_throw()
    {
        var device = new StringDevice();

        Assert.Throws<NotSupportedException>(() => device.Dispatch(InlineRejectionDemo));
    }

    [Test]
    public void Given_a_program_calling_a_macrocallstyle2_convention_procedure_should_throw()
    {
        var device = new StringDevice();

        Assert.Throws<NotSupportedException>(() => device.Dispatch(Style2RejectionDemo));
    }

    [Test]
    public Task Given_a_custom_configuration_should_use_overridden_variable_ranges()
    {
        var configuration = new MacroVariableConfiguration
        {
            ArgumentBaseVariable = 200,
            ReturnValueVariable = 700,
            FirstProgramNumber = 8000
        };
        var device = new StringDevice(configuration);

        device.Dispatch(SubProgramCallDemo, 2.0, 4.0);

        return Verify(device.Code);
    }
}
