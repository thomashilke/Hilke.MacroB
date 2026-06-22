# IlMacroB

A C# to Fanuc Macro B (ISO G-code) compiler for CNC machines. 

IlMacroB parses .NET Common Intermediate Language (CIL/IL) and translates it into Fanuc Macro B code, utilizing standard compiler techniques to generate reasonable and safer result than what would be typically written by hand.

## Overview

This repository contains a set of libraries and tools to compile C# methods into Macro B subprograms. It allows developers to write CNC logic in C# and deploy it to Fanuc-compatible controllers.

### Features
- **IL Parsing**: Extracts MSIL instructions from compiled .NET assemblies.
- **Control Flow Analysis**: Builds Control Flow Graphs (CFG) from IL.
- **SSA Transformation**: Converts code into Static Single Assignment form for optimization.
- **Dead Code Elimination**: Removes unreachable or redundant instructions.
- **Basic Block Merging**: Optimizes the CFG by merging consecutive blocks.
- **Macro B Code Generation**: Produces Fanuc-compatible ISO G-code.
- **CNC Runtime Emulation**: Provides a `Cnc` class to mock CNC-specific operations (moves, math, state) in C#.


## Getting Started

### Requirements
- .NET 10.0 SDK
- A FANUC CNC Series 30i or similar

### Setup
Clone the repository:
```bash
git clone https://github.com/Rollomatic/IlMacroB.git
cd IlMacroB
```

Build the solution:
```bash
dotnet build
```

### Running Samples
To run the demo:
```bash
dotnet run --project samples/Demo/Demo.csproj
```

## Roadmap / TODOs
The following items are planned for future development:
- [ ] Rebuild complex mathematical expressions (brackets, operator precedence).
- [ ] Detect and reconstruct high-level `IF` blocks and `WHILE` loops.
- [ ] Implement function calls as subprograms (`G65`, `M98`).
- [ ] Implement whole program optimization (fallthrough simplification).
- [ ] Add logging and debugging support (PDB source mapping).
- [ ] Implement synchronous communication channels (`G300` etc.).
- [ ] Simulation and expanded unit testing.
- [ ] Support for subprogram argument initialization.

## License
TODO: Add license information.
