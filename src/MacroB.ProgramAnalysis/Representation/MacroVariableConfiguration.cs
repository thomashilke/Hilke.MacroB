namespace Hilke.MacroB.FrontEnd;

// Every macro-variable range this compiler's procedure-call conventions allocate into. All values are
// data, not hardcoded literals in TacConverter/MacroBCodeGenerator, so a caller can retarget them
// (e.g. a machine with different reserved common-variable ranges) without recompiling the toolchain.
public sealed class MacroVariableConfiguration
{
    public static MacroVariableConfiguration Default { get; } = new();

    // Register base for the graph-coloring general-purpose allocator (_registerMap) when compiling the
    // main (Dispatch-invoked) program, and when compiling a MacroCallStyle1 procedure's own locals
    // (safe to share the same base as the main program: G65 pushes an isolated local-variable frame per
    // call, so a Style1 procedure's own #1-33 never overlaps the caller's).
    public int GeneralPurposeRegisterBase { get; init; } = 1;

    // Upper bound on how many registers a MacroCallStyle1 procedure's own graph coloring may use,
    // enforced because G65's pushed local-variable frame is exactly #1-#33 in hardware — exceeding it
    // would silently spill into shared common variables. Not enforced for the main program (no fixed
    // hardware frame backs it).
    public int GeneralPurposeRegisterCount { get; init; } = 33;

    // SubProgramCall (M98) does NOT push an isolated local-variable frame — locals are shared between
    // caller and callee. Each distinct SubProgramCall-convention procedure is therefore given its own
    // disjoint slice of registers (base + discoveryIndex * stride) so its own graph-coloring locals can
    // never collide with the main program's or another procedure's live locals.
    public int SubProgramRegisterBase { get; init; } = 600;

    public int SubProgramRegisterStride { get; init; } = 30;

    // Subprogram-call argument mailbox: argument i is written to ArgumentBaseVariable + i before the
    // call (M98/G65 have no native parameter-passing shared by both conventions' return path).
    public int ArgumentBaseVariable { get; init; } = 100;

    // Single common-variable slot the callee writes its return value to before M99, and the caller
    // reads immediately after the call. At most one call's return value is ever "in flight" at a time
    // (every mailbox write is immediately followed by the call, every read immediately follows the
    // return), so one fixed slot is sufficient even under recursion.
    public int ReturnValueVariable { get; init; } = 500;

    // First auto-assigned subprogram/macro program number; see ProcedureDiscovery.
    public int FirstProgramNumber { get; init; } = 9000;
}
