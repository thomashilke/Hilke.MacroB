# Repository Guidelines

## Project Overview

MacroB compiles C# methods into Fanuc Macro B (ISO G-code) subprograms for CNC machines. Developers write ordinary C# static methods against a mock `Cnc` API (`Cnc.Math`, `Cnc.Machine`, `Cnc.RaiseAlarm`/`Cnc.Stop`), and the compiler reflects the compiled .NET IL of that method, runs it through a real compiler pipeline (CFG, SSA, optimization passes, register allocation), and emits Fanuc-compatible Macro B text.

The project is early-stage: control flow is only lowered to flat `IF[...]GOTO`/`GOTO` with numeric `N`-labels — there is **no** structural `IF`/`WHILE` reconstruction, no subprogram calls (`G65`/`M98`/`M198`), and `src/MacroB.Compiler` is an empty stub project. See "Known Limitations" below before assuming a roadmap item (README.md, todo.org) is implemented.

## Architecture & Data Flow

Pipeline driven end-to-end by `StringDevice.BuildCodeGenerator` (`src/MacroB.Host.FrontEnd/StringDevice.cs`) — read this file first to understand the whole system:

1. **IL parse** — `IlParser.ParseMethod(MethodInfo)` (`src/MacroB.FrontEnd/IlParser.cs`) reflects a compiled method's raw IL bytes into `List<IlInstruction>`, resolving operand tokens via `Module.ResolveMember`/`ResolveString`.
2. **CFG build** — `ControlFlowGraphBuilder.Create(instructions)` (`src/MacroB.FrontEnd/ControlFlowGraphBuilder.cs`) does classic leader-based basic-block partitioning → `ControlFlowGraph<BasicBlock>`.
3. **TAC lowering** — `TacConverter.Convert(cfg)` (`src/MacroB.FrontEnd/TacConverter.cs`) simulates the IL evaluation stack per block (fixpoint via `InOutStackAnalysis` + `DataFlowAnalysisSolver`) and lowers stack-machine IL into three-address `TacInstruction`s → `ControlFlowGraph<TacInstructionBlock>`.
4. **Optimization passes** (`src/MacroB.ProgramAnalysis/Transformation/`, all implement `IControlFlowGraphTransformation<TacInstructionBlock>`), applied in this fixed order by `StringDevice`:
   `StaticSingleAssignmentRenameTransform` (dominance frontiers + phi insertion via `DominanceEngine`, then `SingleStaticAssignmentRecursiveRenamer`) → `ConstantPropagatorTransform` (sparse constant folding) → `DeadCodeEliminationTransform` (copy-prop + purity-aware dead-instruction removal) → `BasicBlockMergeTransform` (fallthrough-chain coalescing).
5. **Code generation** — `MacroBCodeGenerator` (`src/MacroB.BackEnd/MacroBCodeGenerator.cs`) runs `LivenessAnalysis` → `LivenessRangeAnalysis` → `LivenessRangeInterferenceAnalysis` → `UndirectedGraph<SsaVariable>.GreedyColoring` for Macro-B `#n` register allocation, then `BasicBlockOrderAnalysis` (Pettis-Hansen chaining, only cost model is `NaiveCostModel`) to linearize blocks and assign `N`-sequence numbers (multiples of 10). `GenerateCode(params object[] arguments)` emits the final ISO text: arithmetic/trig expressions, `IF[cond]GOTO Nxx`/`GOTO Nxx` (fallthrough elided when the successor matches), `M99` for return, and hardcoded `G01`/`M35`/`M39` for the `Move`/`StartCoolant`/`StopCoolant` intrinsics (matched by `MethodInfo.Name`).

Project/assembly dependency graph (bottom-up):
```
MacroB.Graph  (no deps; graph abstractions + Hilke.DataStructures.UnionFind)
  -> MacroB.ProgramAnalysis  (analyses/transforms/representation over the graph)
    -> MacroB.FrontEnd       (IL parse, CFG build, TAC lowering; also -> Graph)
    -> MacroB.BackEnd        (code generation)
    -> MacroB.Host.FrontEnd  (Cnc DSL + StringDevice; -> FrontEnd + BackEnd)
      -> MacroB.Compiler     (-> FrontEnd + BackEnd; currently NO source files — empty aggregator)
```
Samples and tests depend on `MacroB.Host.FrontEnd` (+ `MacroB.FrontEnd` directly for the low-level `DumpIl` sample).

## Key Directories

- `src/MacroB.Graph/` — generic graph primitives: `IGraph<TVertex>`/`IAdjacencyVertex<TVertex>` (`IGraph.cs`), `UndirectedGraph<TVertex>` (register-interference graph), `GraphExtensions` (`ReversePostOrder`, `DepthFirstIterator`, `GreedyColoring`). Contains a dead `Class1.cs` scaffold file.
- `src/MacroB.FrontEnd/` — IL decoding, CFG construction, and IL→TAC lowering (`IlParser`, `IlInstruction`, `ControlFlowGraphBuilder`, `BasicBlockBuilder`, `BasicBlock`, `InOutStackAnalysis`, `TacConverter`, `BlockTranslationContext`).
- `src/MacroB.ProgramAnalysis/` — the analysis/optimization core, split into subfolders:
  - `Representation/` — TAC IR types: `TacInstruction`, `TacInstructionBlock`, `Operand` (opcode enum), `OperandBase` + subtypes (`Constant`, `SsaVariable`, `JumpTarget`, `FunctionCall`/`IntrinsicFunctionCall`).
  - `Analysis/` — `DominanceEngine`, `DominatorTree`, `DataFlowAnalysisSolver<TState,TVertex>` (generic worklist solver), `LivenessAnalysis`, `LivenessRangeAnalysis`, `LivenessRangeInterferenceAnalysis`, `BasicBlockOrderAnalysis`, `VariableDefinitionUses`, `ICostModel<TVertex>`.
  - `Extensions/` — `ControlFlowGraphExtensions`, `OperandExtensions`, `TacInstructionBlockExtensions`.
  - `Transformation/` — the four optimization passes listed above plus `IControlFlowGraphTransformation<TBlock>`.
  - `ControlFlowGraph.cs` — generic `ControlFlowGraph<TBlock> : IGraph<TBlock>` reused for both `BasicBlock` and `TacInstructionBlock` stages.
- `src/MacroB.BackEnd/` — `MacroBCodeGenerator` (final codegen) and `NaiveCostModel` (the only `ICostModel` implementation).
- `src/MacroB.Host.FrontEnd/` — the public-facing surface: `Cnc.cs` (DSL intrinsics + FANUC hardware constants), `StringDevice.cs` (the compiler driver / `Cnc.IDevice` implementation), `MacroCallConvention[Attribute].cs`, `ProgramEntryPointAttribute.cs`.
- `src/MacroB.Compiler/` — empty aggregator project (`.csproj` only, no `.cs` files); intended future top-level compiler/CLI, not yet started.
- `tests/` — `MacroB.Host.FrontEnd.Tests/`, `MacroB.ProgramAnalysis.Tests/` (both are full-pipeline snapshot tests; neither unit-tests an individual stage — see Testing & QA).
- `samples/Demo/` — canonical end-user API usage (`Program.cs`: define a method, `new StringDevice().Dispatch(method, args...)`, print `device.Code`).
- `samples/DumpIl/` — diagnostic sample exposing every pipeline stage manually (per-block TAC, liveness ranges + register colors, interference edges) for debugging the compiler itself.

## Development Commands

Run from `C:/ProjDotNet/Rollomatic/Hilke.MacroB` (only one `MacroB.slnx`, new `.slnx` solution format, requires .NET 10.0 SDK):

```bash
dotnet restore MacroB.slnx                 # restore (implicit on build/run too)
dotnet build MacroB.slnx                   # build everything
dotnet build src/MacroB.FrontEnd/MacroB.FrontEnd.csproj   # build one project

dotnet test MacroB.slnx                    # run all tests
dotnet test tests/MacroB.Host.FrontEnd.Tests/MacroB.Host.FrontEnd.Tests.csproj
dotnet test tests/MacroB.ProgramAnalysis.Tests/MacroB.ProgramAnalysis.Tests.csproj
dotnet test <csproj> --filter "Given_a_program_calling_dispatch_device_should_generate_valid_macrob_program"
dotnet test --collect:"XPlat Code Coverage"   # coverlet.collector is already referenced

dotnet run --project samples/Demo/Demo.csproj      # prints generated Macro B code for a hardcoded demo program
dotnet run --project samples/DumpIl/DumpIl.csproj  # dumps IL/CFG/TAC/liveness/coloring internals, then final code
```

No `dotnet pack`/publish targets, no CI pipeline (no `.github/workflows`, no `azure-pipelines.yml`) — verification is manual, local `dotnet build`/`dotnet test`.

## Code Conventions & Common Patterns

- **Target framework**: `net10.0` uniformly, `ImplicitUsings=enable`, `Nullable=enable` in every project. No `Directory.Build.props`/`Directory.Packages.props`/`global.json`/committed `.editorconfig` — every `.csproj` repeats its settings. Follow the existing per-project pattern when adding a project rather than introducing central config.
- **Namespaces do not mirror assembly boundaries** — this is the single most surprising convention in the repo: `MacroB.Graph`, `MacroB.FrontEnd`, most of `MacroB.ProgramAnalysis`, and `MacroBCodeGenerator.cs` (in `MacroB.BackEnd`) all declare `namespace Hilke.MacroB.FrontEnd;` regardless of which assembly they live in. `MacroB.Host.FrontEnd/*.cs` instead uses `namespace MacroB.Host.FrontEnd;` (no `Hilke.` prefix), and `src/MacroB.BackEnd/NaiveCostModel.cs` uses yet another namespace, `Hilke.MacroB`. `src/MacroB.Graph/Class1.cs` uses bare `MacroB.Graph`. A couple of files (`BasicBlockOrderAnalysis.cs`, `ICostModel.cs`) declare no namespace at all. When adding new types, match the namespace already used by sibling files in the same folder — do not "fix" this inconsistency as a drive-by change.
- **Extension methods**: two styles coexist — the new C# `extension(...)` block syntax (`GraphExtensions`, `ControlFlowGraphExtensions`, `TacInstructionBlockExtensions`) and classic `this`-parameter extension methods (`OperandExtensions.IsControlFlow`). Prefer the `extension(...)` block style for new extension groups to match the majority convention.
- **Value equality**: `SsaVariable` and `JumpTarget` hand-roll `IEquatable<T>` + `==`/`!=`/`GetHashCode` instead of using `record`. Follow this manual style for other `OperandBase` subtypes rather than converting to records.
- **Optimization passes** implement `IControlFlowGraphTransformation<TBlock>` (`src/MacroB.ProgramAnalysis/Transformation/IControlFlowGraphTransformation.cs`) with a single `Transform(ControlFlowGraph<TBlock>)` method; most mutate blocks in place and return the same graph reference. New passes should follow this interface and be wired into `StringDevice.BuildCodeGenerator`'s fixed pipeline order.
- **Dataflow analyses** are built on the generic worklist solver `DataFlowAnalysisSolver<TState, TVertex>` (`src/MacroB.ProgramAnalysis/Analysis/DataFlowAnalysisSolver.cs`, `SolveForwardFlow`/`SolveBackwardFlow`) — reuse it for new analyses instead of hand-rolling a fixpoint loop (as `InOutStackAnalysis` and `LivenessAnalysis` both do).
- **Error handling**: no `Result`/exception-hierarchy convention — unsupported IL opcodes and invalid enum arguments throw `NotSupportedException`/`InvalidEnumArgumentException` directly (see `TacConverter`, `MacroCallConventionAttribute`). The `Cnc` DSL intentionally throws `NotSupportedException` from every member body (`ThrowSupportedOnlyOnCnc()`); this is by design — those bodies exist only as IL markers recognized by name (`MethodInfo.Name`) in `TacConverter`/`MacroBCodeGenerator`, never actually executed at runtime.
- **The `Cnc` DSL pattern for writing CNC logic**: write a plain `static void` C# method calling `Cnc.Machine.*`/`Cnc.Math.*`/`Cnc.RaiseAlarm`/`Cnc.Stop` plus ordinary C# `if`/`for`/arithmetic (see `samples/Demo/Program.cs`), then compile it via `new StringDevice().Dispatch(method, args...)` (overloads exist for 0–3 parameters only) and read the result off `device.Code`.
- **Dead/unwired scaffolding to be aware of** (do not build new features assuming these work): `MacroCallConventionAttribute` and `ProgramEntryPointAttribute` (`src/MacroB.Host.FrontEnd/`) do **not** inherit `System.Attribute` and have zero usages repo-wide — they cannot currently be applied as `[Attribute(...)]` syntax; `src/MacroB.Graph/Class1.cs` is an empty template leftover; `src/MacroB.Compiler/` has no source files; `TacInstructionBlock.ReplacePredecessor`/`ReplaceSuccessor` in `Representation/TacInstructionBlock.cs` has inverted-looking logic (`if (_successors.Remove(x)) throw`) and appears unused.

## Important Files

- `src/MacroB.Host.FrontEnd/StringDevice.cs` — the canonical end-to-end pipeline wiring; read this first for any cross-cutting change.
- `src/MacroB.Host.FrontEnd/Cnc.cs` — the public DSL surface / FANUC hardware constants (cites FANUC manual B-63944EN/03).
- `src/MacroB.BackEnd/MacroBCodeGenerator.cs` — final ISO/Macro B text emission; the place to extend for new G/M-code intrinsics or control-flow reconstruction.
- `src/MacroB.FrontEnd/TacConverter.cs` — where new IL opcodes must be added (switch/if-chain over `OpCode`, throws `NotSupportedException` on unrecognized ones).
- `src/MacroB.ProgramAnalysis/Transformation/` — where new/adjusted optimization passes go.
- `MacroB.slnx` — solution file (new `.slnx` XML format, not classic `.sln`).
- `README.md`, `todo.org` — roadmap/feature checklist; cross-reference against "Known Limitations" below before trusting an item is implemented.
- `samples/Demo/Program.cs`, `samples/DumpIl/Program.cs` — the two reference usage patterns (end-user API vs. compiler-internals diagnostics).

## Runtime/Tooling Preferences

- Requires **.NET 10.0 SDK** (per README). No `global.json` pins an exact SDK version.
- No centralized package version management; every `PackageReference` sets its own `Version` explicitly.
- `Hilke.DataStructures.UnionFind` (0.1.2) is the one external package used by `MacroB.Graph`/`MacroB.FrontEnd` (for register-range union-find in `LivenessRangeAnalysis`).
- `LangVersion=latest` is set only in the two test projects; other projects rely on the net10.0-implied default.
- `AllowUnsafeBlocks=True` only in `samples/DumpIl` (needed for an unused `stackalloc` demo method — not a pattern to imitate elsewhere).
- No committed `.editorconfig`/ReSharper style config — `IlMacroB.sln.DotSettings.user` only excludes decompiled BCL files from Rider's cache, not a style source. Infer style from surrounding code (PascalCase types/members, `var` for locals).

## Testing & QA

- **Framework**: NUnit 4.6.1 (`NUnit3TestAdapter`, `NUnit.Analyzers`) + `Verify.NUnit` 31.20.0 for snapshot/golden-file assertions on generated Macro B text. No xUnit/MSTest. No mocking framework (nothing to mock).
- **Both test projects are full-pipeline integration tests, not unit tests**: `tests/MacroB.Host.FrontEnd.Tests/UnitTest1.cs` and `tests/MacroB.ProgramAnalysis.Tests/UnitTest1.cs` each define a `StringDeviceTests` class with one `[Test]` (`Given_a_program_calling_dispatch_device_should_generate_valid_macrob_program`) that builds a `StringDevice`, dispatches a sample method, and does `return Verify(device.Code);`. Despite its name, `MacroB.ProgramAnalysis.Tests` references only `MacroB.Host.FrontEnd`, not `MacroB.ProgramAnalysis` — there are **no unit tests for individual stages** (`MacroB.Graph`, `MacroB.ProgramAnalysis` transforms/analyses, `MacroB.FrontEnd` parsing/CFG/TAC, `MacroB.BackEnd` codegen) in isolation.
- **Snapshot approval workflow**: `[SetUp]` disables `DiffEngine`'s interactive diff runner (`DiffRunner.Disabled = true`) so CI/headless runs never launch a diff tool. On mismatch/first run, Verify writes a `*.received.txt` next to the expected `*.verified.txt`; review and rename/copy it to `*.verified.txt` to approve. Verify's naming convention is `{ClassName}.{MethodName}.verified.txt` — note `tests/MacroB.ProgramAnalysis.Tests/Tests.Test1.verified.txt` is a **stale/orphaned** snapshot (name doesn't match the current `StringDeviceTests` class), and `tests/MacroB.Host.FrontEnd.Tests/` has no `.verified.txt` at all — both tests currently require snapshot approval before they can pass.
- New tests exercising a full C# → Macro B scenario should follow the existing pattern: define a `static void` method against the `Cnc` DSL, `new StringDevice().Dispatch(method, args...)`, `return Verify(device.Code);`, with `DiffRunner.Disabled = true` in `[SetUp]`. `Dispatch` only supports 0–3 parameters.
- Coverage collection is available via `coverlet.collector` (`dotnet test --collect:"XPlat Code Coverage"`) but nothing currently enforces a coverage threshold.
- Stray files to be aware of (leftover scaffolding, not real tests): `tests/MacroB.Host.FrontEnd.Tests/UnitTest1.cs~` (uncompiled editor backup) and the unused `FluentAssertions` package reference in `MacroB.ProgramAnalysis.Tests.csproj`.

## Known Limitations

Cross-referenced against `README.md`/`todo.org` roadmap vs. actual `src/` behavior — do not assume these are implemented:

| Roadmap item | Status |
|---|---|
| Rebuild complex math expressions (brackets/precedence) | Not implemented — `MacroBCodeGenerator` emits flat register-based TAC lines only. |
| Detect/reconstruct `IF` blocks | Not implemented — branches lower to raw `IF[cond]GOTO Nxx`/`GOTO Nxx` per basic block. |
| Detect/reconstruct `WHILE` loops | Not implemented — same goto-based backend. |
| Configurable pass-based compiler infrastructure | Not implemented — `src/MacroB.Compiler` has zero source files; the pipeline is hand-wired inline in `StringDevice.BuildCodeGenerator`. |
| Subprogram calls (`G65`/`M98`/`M198`) | Not implemented — zero references anywhere in `src/`. `MacroCallConvention[Attribute]` exist only as unreferenced, non-functional scaffolding. |
| Subprogram argument initialization | Not implemented (depends on the above). |
| Logging / PDB-based debugging | Not implemented. |
| Synchronous communication channel (`G300` etc.) | Not implemented. |
| Simulation / expanded unit testing | Partial — test projects exist but only cover one end-to-end scenario each; no CNC simulator. |
| Whole-program optimization (fallthrough simplification) | Partial — `BasicBlockMergeTransform` + `NaiveCostModel`-driven `BasicBlockOrderAnalysis` exist but are not "whole program". |
</content>
