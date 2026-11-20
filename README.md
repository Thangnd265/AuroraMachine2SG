# AuroraMachine2SG — Extensible Simulation & Custom Machine Development Platform (v2.6.7.6)

Standalone `.NET 8` Windows WPF platform replicating **LG PRI Aurora 2.x SG (`v2.6.7.6`)** so engineers can develop, customize, simulate, and visually debug machine control programs on **any PC without `Aurora.exe`, `PriLicenseCheckerLib`, `DevExpress`, or `.NET` installed**.

## 1. Run Immediately (Zero-Install Portable or Source Mode)

- **Portable Executable (Copy to any Windows 10/11 x64 PC without Aurora or .NET)**:
  `_publish\AuroraMachine2SG_Portable\AuroraMachine2SG.exe`
- **From Source (after editing custom machine C# code)**:
  ```powershell
  dotnet build AuroraMachine2SG.sln
  dotnet run --project tests\AuroraMachine2SG.Tests
  dotnet run --project src\AuroraMachine2SG.App
  ```
- **Re-build Portable Folder after Code Changes**: Run `Publish_Portable.bat`.

## 2. How to Develop a Custom Machine on This Platform

1. Open `src/AuroraMachine2SG.Core/Sdk/EquipmentSdk.cs` and duplicate `CustomStarterMachineSetup : IEquipmentSetup` (or compile a separate `.NET 8` class library referencing `AuroraMachine2SG.Core.dll` and load it at runtime via the yellow **`+ .DLL`** button on the top bar!).
2. Register your machine's devices in `CreateDevices()` and build state-machine flowcharts in `CreateSequences(SequenceContext ctx)` using `SequenceBuilder`:
   - `.AddAction("NODE_ID", ctx => ...)`
   - `.AddDecision("DECIDE_ID", (ctx, branches) => "NEXT_NODE")`
   - `.AddEventAcceptor("WAIT_ID", "EVENT_NAME", ctx => ...)`
   - `.Flow("Init", "NODE_1", "NODE_2", "Final")`
3. Launch the app and monitor what your program is doing in real time:
   - **Bottom `Dev Trace` Dock** (visible across all menus): Live active node per sequence + millisecond `HisLogger` stream.
   - **`Control > Sequence`**: Visual node-and-arrow flowchart with `Run All`, `Pause`, `Step 1 Node`, `Cycle Stop`, `Breakpoint`, `Arm Force Error`, `Publish Event`, and `Speed (0.25x..4x)`.
   - **`Control > Device`**: Interactive Simulation Test Bench (`Force NG`, custom barcode/payload, import existing Aurora `Doc\Data` `_config_*` folders).
