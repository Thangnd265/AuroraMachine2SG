# Implementation Plan: Extensible Aurora Simulation & Custom Machine Development Platform

**Branch**: `002-aurora-dev-platform` | **Date**: 2026-11-20 | **Spec**: [spec.md](spec.md)

## Summary

Upgrade `AuroraMachine2SG` from a single-machine (`RSMS-001`) simulation into an extensible **Aurora Development & Live Observability Platform**:
1. Add `AuroraMachine2SG.Core.Sdk` (`IEquipmentSetup`, `SequenceBuilder`, `SequenceGraph`, `SequenceRunner`, `HisLogger`, `PluginLoader`, `CpkSignalSimulator`) + 3 built-in machine profiles (`SGMachineMultiSequenceSetup`, `SGMachineMultiFixtureSetup`, `CustomStarterMachineSetup`) + dynamic `.dll` plugin loading.
2. Upgrade `Control > Sequence` with a custom WPF `SequenceFlowchartCanvas` rendering real-time directed node graphs (ovals/rectangles/diamonds/hexagons + arrows + loop-backs) with `Run / Pause / Single-Step / Cycle-Stop / Breakpoint / Force-Error / Publish-Event / Speed` controls.
3. Upgrade `Control > Device` with a right-pane **Interactive Device Simulation Test Bench** (`Override Next Result OK/NG`, custom payload, live TX/RX log) + **Import Aurora `Doc/Data` Config Folder**.
4. Add a collapsible bottom **Live Dev Trace (`HisLogger` + Active Sequence Strip)** across `MainWindow` so developers always see what their program is doing from any screen.
5. Publish a zero-install self-contained `win-x64` portable build (`_publish/AuroraMachine2SG_Portable/AuroraMachine2SG.exe`).

## Technical Context

**Language/Version**: C# 12 / `.NET 8.0` (`net8.0` Core/Tests, `net8.0-windows` WPF App)
**Primary Dependencies**: Zero external NuGet packages (`System.Reflection.AssemblyLoadContext` for plugin loading, WPF `DrawingContext` for flowchart rendering)
**Storage**: Local `_data/` JSON/CSV/ZIP + optional import from external Aurora `Doc/Data` directory
**Testing**: Deterministic `[Fact]` runner in `tests/AuroraMachine2SG.Tests`
**Target Platform**: Windows 10/11 `x64` (including self-contained single-folder portable publish)
**Project Type**: `desktop-app` + extensible `Core.Sdk` plugin host

## Constitution Check

- **I. 1:1 Visual & Operational Fidelity**: PASS — Retains full SG Light shell & 25 subviews while upgrading `Sequence` and `Device` to true Aurora developer workstation behavior.
- **II. Zero Proprietary Lock-In**: PASS — Clean-room `.NET 8` SDK + self-contained publish runs on any Windows PC without Aurora or `.NET` installed.
- **III. Test-First Discipline**: PASS — All `SequenceBuilder`, `SequenceRunner` (Single-Step, Decision branching, EventAcceptor, Breakpoint, Force-Error), `HisLogger`, `CpkSignalSimulator`, and `IEquipmentSetup` profile/plugin tests written RED before implementation.
- **IV. Ponytail Simplicity**: PASS — Pure C# state-machine graph + native WPF `FrameworkElement` flowchart renderer (no Graphviz external process needed!).
- **V. Interactive Completeness**: PASS — Every debug button, node click, device test trigger, profile switch, and trace filter wired end-to-end.

## Project Structure

```text
src/
├── AuroraMachine2SG.Core/
│   ├── Sdk/
│   │   ├── SequenceEngine.cs      # SequenceBuilder, SequenceNode, SequenceRunner, HisLogger, CpkSignalSimulator
│   │   └── EquipmentSdk.cs        # IEquipmentSetup, MachineTopology, Built-in Profiles (RSMS-001 MultiSeq, MultiFixture BAY1..4, CustomStarter) & PluginLoader
│   ├── Models/DomainModels.cs
│   ├── Services/                  # MachineRuntime (integrated with SequenceRunner + HisLogger + Profile switcher)
│   └── ViewModels/                # Updated SequenceDevViewModel, DeviceTestBenchViewModel, MainShellViewModel
└── AuroraMachine2SG.App/
    ├── Controls/
    │   ├── VectorControls.cs
    │   └── SequenceFlowchartView.cs # Interactive node-and-arrow flowchart canvas (click node to select/breakpoint/force-error)
    ├── Views/                     # Upgraded ControlViews (Flowchart Debugger + Device Test Bench) & MainWindow (Profile Switcher + Live Dev Trace Dock)
tests/
└── AuroraMachine2SG.Tests/
    └── PlatformSdkAndFlowchartTests.cs # TDD suite for SequenceBuilder, SingleStep, Decision, EventAcceptor, ForceError, Profiles & CpkSignalSimulator
```
