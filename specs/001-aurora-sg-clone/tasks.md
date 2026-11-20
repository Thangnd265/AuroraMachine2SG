---
description: "Task list for AuroraMachine2SG v2.6.7.6 1:1 Equipment Control & HMI Clone"
---

# Tasks: AuroraMachine2SG v2.6.7.6 1:1 Equipment Control & HMI Clone

**Input**: Design documents from `/specs/001-aurora-sg-clone/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/ui-navigation-contract.md

## Format: `[ID] [P?] [Story] Description`

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Initialize `.NET 8` solution (`Core`, `App`, `Tests`) and zero-dependency test harness.

- [X] T001 Create `.NET 8` solution `AuroraMachine2SG.sln` with `src/AuroraMachine2SG.Core/AuroraMachine2SG.Core.csproj`, `src/AuroraMachine2SG.App/AuroraMachine2SG.App.csproj`, and `tests/AuroraMachine2SG.Tests/AuroraMachine2SG.Tests.csproj`
- [X] T002 Implement zero-dependency `[Fact]` test runner and assertion helpers in `tests/AuroraMachine2SG.Tests/TestRunner.cs`
- [X] T003 [P] Implement MVVM primitives (`ObservableObject`, `RelayCommand`) in `src/AuroraMachine2SG.Core/ViewModels/MvvmPrimitives.cs`

---

## Phase 2: Foundational (Core Models, Persistence & Localization)

**Purpose**: Shared domain entities, JSON/CSV/ZIP storage, QR matrix generator, and multilingual dictionary (`en-US`, `ko-KR`, `zh-CN`, `hi-IN`).

- [X] T004 Write failing unit tests for `StorageService`, `QrMatrixGenerator`, and `LocalizationService` in `tests/AuroraMachine2SG.Tests/FoundationTests.cs`
- [X] T005 Create all domain enums and entity models (`EquipmentConfig`, `HiveConfig`, `DeviceEntry`, `StationSlot`, `ProductUnit`, `TeachingRecipe`, `PdcaMeasurementItem`, `AlarmCatalogEntry`, `AlarmHistoryRecord`, `SparePartItem`, `LogConfigItem`) in `src/AuroraMachine2SG.Core/Models/DomainModels.cs`
- [X] T006 [P] Implement deterministic `QrMatrixGenerator` (21x21 finder/timing/payload matrix) in `src/AuroraMachine2SG.Core/Services/QrMatrixGenerator.cs`
- [X] T007 [P] Implement `LocalizationService` supporting `en-US`, `ko-KR`, `zh-CN`, and `hi-IN` UI tokens in `src/AuroraMachine2SG.Core/Services/LocalizationService.cs`
- [X] T008 Implement `StorageService` (auto-seeding `_data/_config_setting/`, `_config_device/`, `_config_recipe/`, `_alarm/`, CSV export, ZIP backup/restore, dual HIVE server/emulator logging) in `src/AuroraMachine2SG.Core/Services/StorageService.cs`

---

## Phase 3: User Story 1 - Shell Navigation, Startup Device Init, Login & Home Production Operation (Priority: P1) 🎯 MVP

**Goal**: Deliver the complete `Aurora 2.x SG Light` shell, HIVE state machine, MultiSequence simulation engine, RBAC login, and 1:1 `Main (Home)` view.

**Independent Test**: Run `AuroraMachine2SG.Tests` for HIVE/Sequence/Shell RBAC logic and launch `AuroraMachine2SG.App` to run live production on `Home`.

### Tests for User Story 1 ⚠️

- [X] T009 [US1] Write failing tests for `HiveStateMachine`, `MultiSequenceEngine`, safety interlocks, and RBAC menu gating in `tests/AuroraMachine2SG.Tests/HiveAndSequenceTests.cs`

### Implementation for User Story 1

- [X] T010 [US1] Implement `HiveStateMachine` (`Idle`, `Running`, `Engineering`, `PlannedDT`, `UnplannedDT`, `Degraded`, QR payload rules, downtime minutes `RepairEnd - HelpRequest`, maintenance counter rules) in `src/AuroraMachine2SG.Core/Services/HiveStateMachine.cs`
- [X] T011 [US1] Implement `MultiSequenceEngine` & central `MachineRuntime` (`HSG` & `MPS` stations, `Loading -> Align -> Process -> Inspection -> Unloading`, CycleTime/UPH/UPD, Screw Torque waveform, Cassette/Tray matrix, LatestHistory) in `src/AuroraMachine2SG.Core/Services/MachineRuntime.cs`
- [X] T012 [US1] Implement `MainShellViewModel` and `HomeViewModel` (6 main menus, RBAC login check, language switch, Door/Mode interlocks, Play/Pause/Stop/EMO/Manual Load/Remove Product commands) in `src/AuroraMachine2SG.Core/ViewModels/ShellAndHomeViewModels.cs`
- [X] T013 [P] [US1] Create `GenericLightSG` WPF resource dictionary (`#102236` header, `Brush.xaml` colors, `StGroupBox20..24`, `StRoundedButton`, `StToggle1/2`, `StToggleStatus`, `StDoorButton`, converters) in `src/AuroraMachine2SG.App/Themes/LightSG.xaml`
- [X] T014 [P] [US1] Implement native WPF vector chart & QR controls (`PieRatioChartControl`, `TorqueCurveControl`, `BarTrendChartControl`, `CpkCapabilityChartControl`, `QrCodeControl`) in `src/AuroraMachine2SG.App/Controls/VectorControls.cs`
- [X] T015 [US1] Implement operational modal dialogs (`WinDeviceInitDialog`, `WinSettingsLoginDialog`, `WinHiveDowntimeDialog`, `WinJogDialog`) in `src/AuroraMachine2SG.App/Dialogs/OperationalDialogs.cs`
- [X] T016 [US1] Implement 1:1 `HomeView.xaml` & `HomeView.xaml.cs` matching `SGMachine/Views/Home/UCHomeView.xaml` in `src/AuroraMachine2SG.App/Views/HomeView.xaml`
- [X] T017 [US1] Implement `MainWindow.xaml` & `MainWindow.xaml.cs` (`Aurora` Top Title/HIVE Bar, 6-Menu Sidebar & Submenu Bar, Content Host, `ScreenLock` overlay, `Ctrl+P` screenshot, Bottom HIVE Status Bar) in `src/AuroraMachine2SG.App/MainWindow.xaml`

---

## Phase 4: User Story 2 - Control, BUSOP & Recipe/Teaching Workflows (Priority: P2)

**Goal**: Deliver all 6 `Control` subviews (`Manual`, `Calibration`, `Device`, `Sequence`, `WMX3 Motion Test`, `BUSOP`) and all 3 `Recipe` subviews (`Recipe`, `Teaching`, `Model`).

**Independent Test**: Verify Teaching tab/card add/edit/clone/save tests pass and all `Control` & `Recipe` subviews operate interactively.

### Tests for User Story 2 ⚠️

- [X] T018 [US2] Write failing tests for `ControlViewModel` (device toggle, axis jog/move, BUSOP judgment) and `RecipeViewModel` (`WMX3PositionRecipe` tab/card edit, `Clone To`, persistence) in `tests/AuroraMachine2SG.Tests/ControlAndRecipeTests.cs`

### Implementation for User Story 2

- [X] T019 [US2] Implement `ControlViewModel` and `RecipeViewModel` in `src/AuroraMachine2SG.Core/ViewModels/ControlAndRecipeViewModels.cs`
- [X] T020 [P] [US2] Implement `ControlViews.xaml` (`Manual`, `Calibration`, `Device`, `Sequence`, `WMX3 Motion Test`, `BUSOP` Force/LoadCell/Vision T31) in `src/AuroraMachine2SG.App/Views/ControlViews.xaml`
- [X] T021 [P] [US2] Implement `RecipeViews.xaml` (`MainRecipe`/`CameraRecipe`, `WMX3PositionRecipe` multi-tab Teaching card editor with `Position List` & `Clone To` dialogs, `MetalModel` editor) in `src/AuroraMachine2SG.App/Views/RecipeViews.xaml`

---

## Phase 5: User Story 3 - Monitoring, FATP Cpk, PDCA & Performance Metrics (Priority: P3)

**Goal**: Deliver exact `SpcCalculator` and all 5 `Monitoring` subviews (`WMX3 IO`, `FATP Cpk`, `PDCADataMonitoring`, `PerformanceMetrics`, `LoadFactor`) with CSV export.

**Independent Test**: Verify `SpcCalculator` unit tests across normal & edge cases and verify `FATP Cpk` & `PerformanceMetrics` filtering + CSV export.

### Tests for User Story 3 ⚠️

- [X] T022 [US3] Write failing tests for `SpcCalculator` (`Cp`, `Cpu`, `Cpl`, `Cpk`, `UPH`, $N=0$, $N=1$, inverted spec) and `MonitoringViewModel` filtering/CSV export in `tests/AuroraMachine2SG.Tests/SpcAndMonitoringTests.cs`

### Implementation for User Story 3

- [X] T023 [US3] Implement `SpcCalculator` matching `SGComponent.CPK.SpcCalculator` in `src/AuroraMachine2SG.Core/Services/SpcCalculator.cs`
- [X] T024 [US3] Implement `MonitoringViewModel` (`WMX3 IO` 32x2 matrix, `FATP Cpk`, `PDCADataMonitoring`, `PerformanceMetrics` OEE/calendar, `LoadFactor`) in `src/AuroraMachine2SG.Core/ViewModels/MonitoringViewModel.cs`
- [X] T025 [US3] Implement `MonitoringViews.xaml` matching `UCFATPCpk`, `UCPerformanceMetricsView`, `UCWMX3IOMonitoring`, `PDCADataView`, and `UCMotorLoadFactor` in `src/AuroraMachine2SG.App/Views/MonitoringViews.xaml`

---

## Phase 6: User Story 4 - Alarm Management, HIVE Maintenance & System Settings (Priority: P4)

**Goal**: Deliver all 5 `Alarm` subviews (`AlarmHistory`, `Tossing List`, `NG List`, `AlarmDescription`, `AlarmValidationReport`) and all 5 `Setting` subviews (`EquipSetting`, `SpareParts`, `LogSetting`, `BackUp`, `Shortcut`).

**Independent Test**: Verify `SpareParts` threshold alarm triggering, `String Generator`, bilingual validation, ZIP backup/restore, and multilingual alarm search.

### Tests for User Story 4 ⚠️

- [X] T026 [US4] Write failing tests for `AlarmViewModel` (multilingual search, validation report, tossing/NG filter) and `SettingViewModel` (`SpareParts` threshold alarm, `String Generator`, 20-slot `LogSetting`, ZIP backup) in `tests/AuroraMachine2SG.Tests/AlarmAndSettingTests.cs`

### Implementation for User Story 4

- [X] T027 [US4] Implement `AlarmViewModel` and `SettingViewModel` in `src/AuroraMachine2SG.Core/ViewModels/AlarmAndSettingViewModels.cs`
- [X] T028 [P] [US4] Implement `AlarmViews.xaml` (`AlarmHistory`, `Tossing List`, `NG List`, `AlarmDescription`, `AlarmValidationReport`) in `src/AuroraMachine2SG.App/Views/AlarmViews.xaml`
- [X] T029 [P] [US4] Implement `SettingViews.xaml` (`EquipSetting`, `SpareParts` banded grid, `LogSetting`, `BackUp`, `Shortcut`) in `src/AuroraMachine2SG.App/Views/SettingViews.xaml`

---

## Phase 7: Polish, Integration & Convergence Verification

**Purpose**: Wire all 25 subviews into `MainWindow`, verify clean build, run full test suite, smoke-launch WPF app, and run `/speckit-converge`.

- [X] T030 Wire all `Control`, `Recipe`, `Alarm`, `Monitoring`, and `Setting` views into `src/AuroraMachine2SG.App/MainWindow.xaml` and add `README.md`
- [X] T031 Execute full `dotnet build`, `dotnet run --project tests/AuroraMachine2SG.Tests`, smoke-launch `AuroraMachine2SG.App` with automated UI self-check/screenshot verification, and run `/speckit-converge`

---

## Dependencies & Execution Order

- **Phase 1 (Setup)** -> **Phase 2 (Foundational)** -> **Phase 3 (US1 Shell & Home MVP)** -> **Phases 4–6 (US2 Control/Recipe, US3 Monitoring/CPK, US4 Alarm/Setting)** -> **Phase 7 (Polish & Converge)**.
- Within every phase, test tasks (`T004`, `T009`, `T018`, `T022`, `T026`) MUST execute and FAIL before their corresponding production implementation tasks.
