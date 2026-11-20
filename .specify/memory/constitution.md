# AuroraMachine2SG Constitution

## Core Principles

### I. 1:1 Visual & Operational Fidelity
Every screen, color token, navigation hierarchy, control state, and workflow MUST faithfully match `AuroraMachine2SG_v2.6.7.6` (`RSMS-001` on Aurora 2.x SG Light theme):
- Top Shell Bar (`#102236` / `#37404E`), HIVE Play/Pause/Stop indicator (`#8DD471` / `#518FFF` / `#E15358`), Open Folder & JOG buttons, Weekly/Monthly Maintenance Counter with dynamic QR code, Language switcher (`en-US`, `ko-KR`, `zh-CN`, `hi-IN`), Device Active badges, and Bottom Status Bar (`UCHiveStatusIndicator` per station + Virtual Keyboard toggle).
- Exact 6 Main Menus and Submenus: `Main` (`Home`), `Control` (`Manual`, `Calibration`, `Device`, `Sequence`, `WMX3 Motion Test`, `BUSOP`), `Recipe` (`Recipe`, `Teaching`, `Model`), `Alarm` (`AlarmHistory`, `Tossing List`, `NG List`, `AlarmDescription`, `AlarmValidationReport`), `Monitoring` (`WMX3 IO`, `FATP Cpk`, `PDCADataMonitoring`, `PerformanceMetrics`, `LoadFactor`), and `Setting` (`EquipSetting`, `SpareParts`, `LogSetting`, `BackUp`, `Shortcut`).
- Role-based permission gating (`PermissionLevel.NONE` for `Main`, `PermissionLevel.LEVEL5` for protected menus via `Settings Login` badge/password modal) and safety interlocks (Door Open/Lock and mode switches disabled while sequences run).

### II. Zero Proprietary Lock-In & Self-Contained Execution
The application MUST compile and run standalone on `.NET 8` Windows (`net8.0-windows` WPF) with zero dependency on closed-source `Aurora.exe`, commercial `DevExpress`, `PriLicenseCheckerLib`, MongoDB/MySQL servers, or vendor hardware DLLs (`WMX3`, `AXL`, `Cognex`, `Keyence`, 32-bit `mwReader.dll`). All device, vision, screw torque, RFID/badge, Hermes, BALI/PDCA, and HIVE 3.x interactions MUST run deterministically via an integrated simulation & local JSON/CSV persistence layer.

### III. Test-First Discipline (NON-NEGOTIABLE)
All core logic—SPC/CPK statistics (`Cp`, `Cpu`, `Cpl`, `Cpk`, `UPH`), HIVE 3.x state transitions (`Idle`, `Running`, `Engineering`, `PlannedDT`, `UnplannedDT`, `Degraded`) and downtime/QR rules, MultiSequence station flow (`HSG` & `MPS` stations), Teaching/Recipe persistence, SpareParts lifespan alarm evaluation, and ViewModel command interlocks—MUST follow strict TDD: write failing tests first, verify RED, implement minimal code to pass GREEN, then refactor.

### IV. Ponytail Simplicity & Native Platform First
Prefer native WPF controls (`ResourceDictionary`, `DataGrid`, `TabControl`, `UniformGrid`, `Canvas`/`Polyline`/`Path` vector charts, `System.Text.Json`) over external UI or charting packages. Avoid unrequested abstractions, single-implementation interfaces where plain classes suffice, and dead placeholder controls.

### V. End-to-End Interactive Completeness
Every visible button, toggle, mode selector, table action, and modal dialog (`WinDeviceInit`, `WinSettingsLogin`, `WinManualDT`, `WinMaintenanceCheckList`, `WinReportEnd`, `WinJog`, `Clone To` / `Position List`, `Export CSV`, `Ctrl+P` Screenshot, `ScreenLock`) MUST perform real state updates and persist or export data cleanly without unhandled exceptions.

## Technical Stack & Architecture Constraints

- **Runtime & UI**: C# 12 / `.NET 8` (`net8.0-windows`), WPF (`UseWPF=true`), MVVM architecture.
- **Solution Structure**:
  - `src/AuroraMachine2SG.Core`: Pure `.NET 8` class library containing models, services, sequence engine, HIVE 3.x state machine, SPC/CPK calculator, QR matrix generator, JSON/CSV persistence, and ViewModels (100% unit-testable headless).
  - `src/AuroraMachine2SG.App`: WPF executable hosting `GenericLightSG` resource dictionaries, custom vector charts, main shell, 6 menu views, and HIVE/Machine dialogs.
  - `tests/AuroraMachine2SG.Tests`: xUnit test suite verifying all core engines and ViewModels.
- **Data Storage**: Local `_data/` directory mirroring `_config_setting/`, `_config_device/`, `_config_recipe/`, `_alarm/`, `_backup/`, and `_export/` in UTF-8 JSON and CSV formats.

## Development Workflow & Quality Gates

- Follow GitHub Spec Kit (`constitution` -> `specify` -> `plan` -> `tasks` -> `implement` -> `converge`).
- Every implementation slice MUST pass `dotnet test` (0 failures, 0 errors) and `dotnet build` before marking tasks complete.
- Final delivery MUST pass `/speckit-converge` with zero CRITICAL/HIGH gaps and verified startup of `AuroraMachine2SG.App`.

## Governance

This Constitution supersedes ad-hoc shortcuts. Any change that introduces proprietary dependencies, breaks 1:1 menu/workflow parity with `AuroraMachine2SG_v2.6.7.6`, or bypasses automated tests is prohibited unless explicitly amended by user directive with a semantic version bump.

**Version**: 1.0.0 | **Ratified**: 2026-11-20 | **Last Amended**: 2026-11-20
