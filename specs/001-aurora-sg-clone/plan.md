# Implementation Plan: AuroraMachine2SG v2.6.7.6 1:1 Equipment Control & HMI Clone

**Branch**: `001-aurora-sg-clone` | **Date**: 2026-11-20 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/001-aurora-sg-clone/spec.md`

## Summary

Build a standalone, license-free `.NET 8` Windows WPF desktop application (`AuroraMachine2SG`) that reproduces the visual design (`GenericLightSG` theme), 6-menu / 25-subview navigation hierarchy, HIVE 3.x state & QR workflows, MultiSequence station simulation, WMX3 position teaching, FATP CPK / PDCA analytics, multilingual alarm catalog, and JSON/CSV/ZIP persistence of `AuroraMachine2SG_v2.6.7.6` (`RSMS-001`) using pure `.NET 8` + native WPF without proprietary `Aurora.exe` or `DevExpress` dependencies.

## Technical Context

**Language/Version**: C# 12 / `.NET 8.0` (`net8.0` for Core & Tests, `net8.0-windows` for WPF App)

**Primary Dependencies**: Zero external NuGet packages — pure `.NET 8` Base Class Library (`System.Text.Json`, `System.IO.Compression`) + Windows Presentation Foundation (`PresentationFramework`, `PresentationCore`, `WindowsBase`)

**Storage**: Local UTF-8 JSON & CSV files under `_data/` (`_config_setting/`, `_config_device/`, `_config_recipe/`, `_alarm/`, `_backup/`, `_export/`, `HIVE/log/`, `HIVE/emulator_log/`)

**Testing**: Built-in deterministic `[Fact]` test runner in `tests/AuroraMachine2SG.Tests` (`dotnet run --project tests/AuroraMachine2SG.Tests`) executing headless against `AuroraMachine2SG.Core`

**Target Platform**: Windows 10/11 `x64` Desktop (`WPF`)

**Project Type**: `desktop-app` (MVVM architecture with separated testable Core library)

**Performance Goals**: Instant startup (<1.5s), smooth 60fps UI responsiveness during 500ms–1000ms station simulation ticks, <80MB RAM footprint

**Constraints**: Offline-capable build & execution; zero commercial SDKs or obfuscated binaries; 1:1 visual palette & workflow parity with `AuroraMachine2SG_v2.6.7.6`

**Scale/Scope**: 1 Shell Window, 6 Main Menus, 25 Subviews, 7 Operational Dialogs, 19 Simulated Devices, full HIVE 3.x + SPC/CPK + MultiSequence engine

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **I. 1:1 Visual & Operational Fidelity**: PASS — `LightSG` resource dictionary, exact hex tokens, all 6 menus / 25 subviews, and HIVE/Teaching/CPK dialogs mapped in [contracts/ui-navigation-contract.md](contracts/ui-navigation-contract.md).
- **II. Zero Proprietary Lock-In**: PASS — 0 external binaries or commercial NuGet packages.
- **III. Test-First Discipline**: PASS — `AuroraMachine2SG.Core` isolates all calculators, state machines, sequence logic, persistence, and ViewModels for Red-Green-Refactor TDD prior to implementation.
- **IV. Ponytail Simplicity & Native Platform First**: PASS — Native WPF vector Canvas/Path controls replace 4 separate third-party chart/QR libraries; `System.Text.Json` + `ZipFile` replace MongoDB/MySQL/custom archivers.
- **V. End-to-End Interactive Completeness**: PASS — Every view and modal binds to live commands and shared `MachineRuntime` state.

## Project Structure

### Documentation (this feature)

```text
specs/001-aurora-sg-clone/
├── plan.md              # Implementation plan
├── research.md          # Phase 0 architecture & dependency decisions
├── data-model.md        # Domain entities, enums, and validation rules
├── quickstart.md        # Build, test, and verification scenarios
├── contracts/           # UI navigation & modal contracts
│   └── ui-navigation-contract.md
└── tasks.md             # Dependency-ordered task list (/speckit-tasks)
```

### Source Code (repository root)

```text
AuroraMachine2SG.sln
src/
├── AuroraMachine2SG.Core/
│   ├── Models/          # Enums, EquipmentConfig, HiveModels, StationModels, RecipeModels, PdcaModels, AlarmModels, SparePartModels
│   ├── Services/        # SpcCalculator, QrMatrixGenerator, HiveStateMachine, MultiSequenceEngine, StorageService, LocalizationService, MachineRuntime
│   └── ViewModels/      # ObservableObject, RelayCommand, MainShellViewModel, HomeViewModel, ControlViewModel, RecipeViewModel, AlarmViewModel, MonitoringViewModel, SettingViewModel, DialogViewModels
└── AuroraMachine2SG.App/
    ├── Themes/          # LightSG.xaml (exact Brush.xaml + GroupBox20..24 + Button/Toggle/Tab/DataGrid styles)
    ├── Controls/        # VectorCharts.cs (Pie, TorqueCurve, BarTrend, CpkChart), QrCodeView.cs
    ├── Dialogs/         # WinDeviceInit, WinSettingsLogin, WinHiveDowntime, WinJog, WinPositionClone
    ├── Views/           # HomeView, ControlViews, RecipeViews, AlarmViews, MonitoringViews, SettingViews
    ├── MainWindow.xaml  # Aurora 2.x SG Shell (Top Bar, Left/Sub Menu, Content Host, ScreenLock Overlay, Status Bar)
    └── App.xaml
tests/
└── AuroraMachine2SG.Tests/
    ├── TestRunner.cs    # Lightweight [Fact] assertion harness
    ├── SpcAndQrTests.cs # TDD tests for SpcCalculator & QrMatrixGenerator
    ├── HiveAndSeqTests.cs # TDD tests for HIVE 3.x state machine, interlocks & MultiSequenceEngine
    └── PersistenceAndVmTests.cs # TDD tests for JSON/CSV storage, Teaching clone, SpareParts alarms & Shell RBAC
```

**Structure Decision**: Clean 3-project `.NET 8` solution (`Core` + `App` + `Tests`) maximizing headless TDD coverage in `Core` while keeping XAML rendering lean and declarative in `App`.

## Complexity Tracking

No Constitution violations — table omitted.
