# MacroB

MacroB compiles ordinary C# methods into Fanuc Macro B (ISO G-code) subprograms for CNC machines.

Write CNC logic as a plain C# `static void` method against a mock `Cnc` API (`Cnc.Math`, `Cnc.Machine`, `Cnc.RaiseAlarm`/`Cnc.Stop`). MacroB reflects the compiled .NET IL of that method, runs it through a real compiler pipeline (control-flow graph, SSA, optimization passes, register allocation), and emits Fanuc-compatible Macro B text.

## Sample

```csharp
using MacroB.Host.FrontEnd;

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

var device = new StringDevice();
device.Dispatch(IsoProgramDemo, 1, 3);
Console.WriteLine(device.Code);
```

Output:

```
#1 = 1
#7 = 3
IF[#1 GT 5]GOTO 50
N20 #1 = #1 * 2
M35
#3 = 0
#2 = 1
N30 IF[#2 LT 3 EQ 0]GOTO 60
N40 G01 X[SIN[#1 + #7]] Y#3 Z34 F500
#3 = #3 + 0.8660254037844386
#2 = #2 + 1
GOTO 30
N50 #1 = #1 - 5
GOTO 20
N60 M39
M99
```

## Sample projects

- **`samples/Demo`** — the reference end-user usage pattern above: define a method, dispatch it through `StringDevice`, print the generated Macro B code.
- **`samples/DumpIl`** — a diagnostic sample for debugging the compiler itself: dumps the raw IL, the CFG/TAC per basic block, liveness ranges with allocated registers, and the interference graph, before printing the final generated code.

## Build and run

Requires the .NET 10.0 SDK.

```bash
dotnet build MacroB.slnx

dotnet run --project samples/Demo/Demo.csproj
dotnet run --project samples/DumpIl/DumpIl.csproj
```
