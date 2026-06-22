namespace IlMacroB.ProgramAnalysis.Tests;

public class Tests
{
    [Test]
    public Task Test1()
    {
        var s = "Hello";
        return Verify(s);
    }
}
