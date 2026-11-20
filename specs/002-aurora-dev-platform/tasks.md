---
description: "Tasks for 002-aurora-dev-platform"
---

# Tasks: Extensible Aurora Simulation & Custom Machine Development Platform

## Phase 1: TDD Tests for Platform SDK, Sequence Flowchart Engine & Profiles (RED)

- [ ] T001 Write failing unit tests for `SequenceBuilder` / `SequenceRunner` (`Action`, `Decision`, `EventAcceptor`, `SingleStep`, `Breakpoint`, `ForceError`, `CycleStop`), `HisLogger`, `CpkSignalSimulator` (Ornstein–Uhlenbeck), and `IEquipmentSetup` profile switching (`MultiSequence`, `MultiFixture BAY1..4`, `CustomStarter` & assembly plugin discovery) in `tests/AuroraMachine2SG.Tests/PlatformSdkAndFlowchartTests.cs`

## Phase 2: Core Platform SDK & Engine Implementation (GREEN)

- [ ] T002 Implement `HisLogger`, `CpkSignalSimulator`, `SequenceBuilder`, `SequenceGraph`, `SequenceNode`, and `SequenceRunner` in `src/AuroraMachine2SG.Core/Sdk/SequenceEngine.cs`
- [ ] T003 Implement `IEquipmentSetup`, `MachineTopology`, `PluginLoader`, and 3 built-in machine profiles (`SGMachineMultiSequenceSetup`, `SGMachineMultiFixtureSetup`, `CustomStarterMachineSetup`) + external `Doc/Data` config importer in `src/AuroraMachine2SG.Core/Sdk/EquipmentSdk.cs`
- [ ] T004 Integrate `SequenceRunner`, `HisLogger`, `IEquipmentSetup` profile switching, and `DeviceTestBench` simulation commands into `src/AuroraMachine2SG.Core/Services/MachineRuntime.cs` and `src/AuroraMachine2SG.Core/ViewModels/AllFeatureViewModels.cs`

## Phase 3: Visual Flowchart Debugger, Device Test Bench & Live Dev Trace UI

- [ ] T005 [P] Implement interactive `SequenceFlowchartView` (node shapes by kind, live status colors, hit count & ms badges, directed arrows & loop-back arcs, click-to-select node) in `src/AuroraMachine2SG.App/Controls/SequenceFlowchartView.cs`
- [ ] T006 Upgrade `ControlViews.xaml` & `ControlViews.xaml.cs` (`Sequence` live flowchart debugger with `Run / Pause / Single Step / Cycle Stop / Breakpoint / Force Error / Publish Event / Sim Speed` + `Device` interactive right-pane Simulation Test Bench) in `src/AuroraMachine2SG.App/Views/ControlViews.xaml`
- [ ] T007 Upgrade `MainWindow.xaml` & `MainWindow.xaml.cs` with Top-Bar **Machine Profile / Plugin `.dll` / Import Data Folder** selector and collapsible bottom **Live Dev Trace (`HisLogger` + Active Sequence Monitor)** dock in `src/AuroraMachine2SG.App/MainWindow.xaml`

## Phase 4: Self-Contained Portable Publish, Visual Verification & Convergence

- [ ] T008 Publish self-contained `win-x64` portable package (`_publish/AuroraMachine2SG_Portable/AuroraMachine2SG.exe`), run full unit test suite + live screenshot verification of `Sequence` flowchart debugger & `Device` test bench, and run `/speckit-converge`
