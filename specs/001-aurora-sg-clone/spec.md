# Feature Specification: AuroraMachine2SG v2.6.7.6 1:1 Equipment Control & HMI Clone

**Feature Branch**: `001-aurora-sg-clone`

**Created**: 2026-11-20

**Status**: Approved

**Input**: User description: "sử dụng rea phân tíhc phần mềm E:\CTQ\LGPRICTQ\AuroraMacine2SG_v2.6.7.6 và tạo một chương trình y hệt cho tôi — triển khai, tôi muốn giao diện và cách sử dụng giống y hệt nhé"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Shell Navigation, Startup Device Init, Login & Home Production Operation (Priority: P1)

An equipment operator or line engineer launches the `RSMS-001` machine software, initializes devices through the startup Device Initialization modal, monitors live MultiSequence production (`HSG` & `MPS` stations) on the `Main (Home)` dashboard, operates Play / Pause / Stop, Door Open / Lock, Manual Load / UnLoad, Machine Mode switches (`CPK`, `PDCA`, `PDCA OFF`, `GRR`, `DRY-RUN`, `HERMES`, `MPS Use`), views real-time Cycle Time / UPH / UPD, Pie Ratio chart, Screw Torque-Angle chart, Cassette & Tray matrix, Latest History, Daily/Weekly UPH/UPD charts, switches UI language (`en-US`, `ko-KR`, `zh-CN`, `hi-IN`), and logs in via Badge swipe or password (`Settings Login`) to unlock level-gated menus (`Control`, `Recipe`, `Alarm`, `Monitoring`, `Setting`).

**Why this priority**: The `Aurora` shell, HIVE top/bottom status bars, Login gate, and `Main (Home)` screen form 100% of daily operator interaction and anchor every other menu.

**Independent Test**: Launch the app, complete `WinDeviceInit`, verify `Home` renders all panels with SG Light palette, press `Play` to watch products advance across `Loading -> Align -> Process -> Inspection -> Unloading` (and `MPSLoading -> MPSProcess` when `MPS Use` is active) while updating Cycle Time, UPH, UPD, Screw Torque curve, Tray/Cassette cells, Latest History, and charts; attempt to open `Control` while logged out (triggers `Settings Login`), swipe badge/login as `L5/L8`, and confirm protected menus open.

**Acceptance Scenarios**:

1. **Given** the application starts in `Idle` state with `Door Lock` engaged, **When** the operator clicks `Play` on the top HIVE indicator, **Then** HIVE status transitions to `Running` (`#8DD471`), `Door Open`/`Door Lock` and mode radio buttons become disabled, and simulated products flow sequentially through active stations updating UPH/UPD/Cycle Time, Screw Torque curve, Cassette/Tray grid, and Latest History.
2. **Given** sequences are running, **When** the operator clicks `Stop` (or `EMO TEST`), **Then** sequences halt safely, HIVE Manual Downtime (`WinManualDT`) or Auto-Triggered Alarm (`WinAutoTriggeredDT` / `WinEmergency`) workflow activates with a generated QR code in the top maintenance/QR indicator, and `Door Open` becomes operable once stopped.
3. **Given** current user permission is `NONE` (logged out), **When** the user clicks `Control`, `Recipe`, `Alarm`, `Monitoring`, or `Setting`, **Then** `Settings Login` (`WinSettingsLogin`) opens supporting simulated Badge swipe (`Operator`, `Line Leader`, `Engineer L5`, `Admin L8`) and password verification before granting access.

---

### User Story 2 - Control, BUSOP & Recipe/Teaching Workflows (Priority: P2)

A process engineer accesses `Control` (`Manual`, `Calibration`, `Device`, `Sequence`, `WMX3 Motion Test`, `BUSOP`) and `Recipe` (`Recipe`, `Teaching`, `Model`) to jog servo axes (`JOG` popup & `WMX3 Motion Test`), activate/deactivate or test individual simulated devices (`BCR`, `Vision`, `RFID`, `ScrewDriver`, `Hermes`, `BALI`, `HIVE`, `WMX3`), inspect/execute sequences, run BUSOP standard verification (`Force`, `LoadCell`, `Static/Dynamic Vision T31`), and edit/clone/save multi-tab `WMX3PositionRecipe` teaching cards (`Loading`, `Processing`, `Unloading`, `Align_Ready`, `Align`, `Align_Unload`) plus `MainRecipe`, `CameraRecipe`, and `MetalModel`.

**Why this priority**: Engineering setup, axis teaching, device diagnostics, and recipe management are essential to configure and operate the machine identically to `AuroraMachine2SG`.

**Independent Test**: Log in at `LEVEL5+`, open `Control -> WMX3 Motion Test` and `JOG` modal to jog axes and see live coordinates update; open `Recipe -> Teaching`, add/edit/clone position tabs and axis target coordinates, click `Save All`, restart or reload, and verify persisted teaching values.

**Acceptance Scenarios**:

1. **Given** `Recipe -> Teaching` is open, **When** the engineer edits target position/velocity/accel on a `WMX3` teaching card, uses `Position List` or `Clone To` across tabs, and clicks `Save All`, **Then** changes persist to local JSON recipe files and reload identically.
2. **Given** `Control -> Device` or `Control -> BUSOP` is open, **When** the engineer toggles device activation or triggers a BUSOP test (`Force`, `LoadCell`, `Vision T31`), **Then** simulated measurements, pass/fail judgments, and top-bar device status indicators update immediately.

---

### User Story 3 - Monitoring, FATP Cpk, PDCA & Performance Metrics (Priority: P3)

A quality/CTQ engineer opens `Monitoring` (`WMX3 IO`, `FATP Cpk`, `PDCADataMonitoring`, `PerformanceMetrics`, `LoadFactor`) to inspect live digital I/O matrices, filter PDCA inspection items by date/hour (`Today`, `All`, multi-select items, `Apply Selected Items`), view `Cp`, `Cpu`, `Cpl`, `Cpk`, `Mean`, `StdDev`, `USL`/`LSL` capability charts, inspect OEE/Availability/Downtime `PerformanceMetrics` by station and date calendar, monitor servo `LoadFactor`, and export reports to CSV.

**Why this priority**: CTQ/FATP statistical process control (`CPK` & `PDCA`) and OEE metrics are core value pillars of `AuroraMachine2SG` v2.6.7.x.

**Independent Test**: Open `Monitoring -> FATP Cpk`, select multiple measurement items, click `Apply Selected Items`, verify `Cp`/`Cpk`/`Mean`/`StdDev`/`UPH` match exact sample-standard-deviation formulas, and click `Export CSV` to verify a valid CSV file is written to disk.

**Acceptance Scenarios**:

1. **Given** measurement data points with defined `LSL` and `USL`, **When** the user filters a date/hour range and clicks `Apply Selected Items` in `FATP Cpk`, **Then** each selected item renders a capability chart with `LSL`/`USL` lines and accurate `N`, `Mean`, `StdDev`, `Cp`, `Cpk`, `Cycles`, and `UPH`.
2. **Given** `WMX3 IO` is open, **When** output bits are toggled or sequences run, **Then** 32-channel Input/Output LED indicators reflect live state changes.

---

### User Story 4 - Alarm Management, HIVE Downtime/Maintenance & System Settings (Priority: P4)

A maintenance technician or administrator uses `Alarm` (`AlarmHistory`, `Tossing List`, `NG List`, `AlarmDescription`, `AlarmValidationReport`) and `Setting` (`EquipSetting`, `SpareParts`, `LogSetting`, `BackUp`, `Shortcut`) plus HIVE maintenance dialogs (`WinMaintenanceCheckList`, `WinRepairConfirm`, `WinReportEnd`, `ScreenLock`, `Ctrl+P` screenshot) to diagnose multilingual alarm solutions, track tossing/NG history, manage consumable spare-part lifespans with bilingual `en;zh` names and alarm thresholds, reset weekly/monthly maintenance counters, configure equipment/HIVE dual-endpoint settings, run backups, and capture screenshots.

**Why this priority**: Completes 100% menu, popup, HIVE 3.x compliance, and administrative parity with v2.6.7.6.

**Independent Test**: Trigger an alarm or spare-part limit exceedance, verify QR code + alarm banner + `AlarmHistory` entry appear, complete Badge scan -> `Repair Confirm` -> `Report End` (calculating downtime minutes), verify HIVE returns to `Idle`, and test `SpareParts` save/reload + `Ctrl+P` screenshot capture.

**Acceptance Scenarios**:

1. **Given** a consumable item in `Setting -> SpareParts` exceeds its usage limit while `Use Alarm` is enabled, **When** a production cycle increments its count, **Then** the configured alarm triggers, highlights the row, and logs to `AlarmHistory`.
2. **Given** the machine enters Planned Maintenance or Unplanned Downtime, **When** the operator swipes badge, checks all items in `Maintenance CheckList` (or submits `Report End` with bilingual cause/part), **Then** Weekly/Monthly maintenance counters or HIVE downtime logs update and persist to disk.

---

### Edge Cases

- **Door / Mode Interlock while Running**: Attempting to open the door, switch machine modes (`CPK`/`PDCA`/`GRR`/`DRY-RUN`), or jog axes while `IsSequenceRunning` is true MUST be blocked or disabled.
- **Insufficient CPK Sample Size ($N < 2$) or Invalid Spec ($USL \le LSL$)**: `SpcCalculator` MUST return `0` / safe fallback without `NaN` crashes or division-by-zero exceptions.
- **Maintenance Partial Checklist Submission**: Per v2.6.6.0 rules, `Submit` in `WinMaintenanceCheckList` is always allowed, but `Maintenance Counter` resets ONLY when 100% of checklist items are checked after badge scan.
- **Empty Station Product Removal**: Right-clicking a station card on `Home` (`Remove Product`) when no product is present MUST be safely ignored without throwing errors.
- **Corrupt or Missing Local JSON Configs**: First run or missing `_data/` files MUST auto-seed clean default configs matching `Doc/Data/_config_*` schemas.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST replicate the `Aurora 2.x SG Light` shell layout: dark navy top bar (`#102236`), HIVE `Play`/`Pause`/`Stop` toggle group, `Open Folder` & `JOG` buttons, `Weekly`/`Monthly` maintenance countdown with live QR code, language selector (`en-US`, `ko-KR`, `zh-CN`, `hi-IN`), active device status pills, collapsible/selectable 6-item main menu (`Main`, `Control`, `Recipe`, `Alarm`, `Monitoring`, `Setting`) with submenus, and bottom station HIVE status bar + virtual keyboard toggle.
- **FR-002**: System MUST provide the startup `Device Initialization` dialog (`WinDeviceInit`) listing all registered `RSMS-001` devices (`BALIConnDevice`, `HIVE`, `SGML`, `BCR`, `KeyenceVisionIF`, `HermesUpstream`, `HermesDownstream`, `CardReader`, `RFID`, `WMX3_ENGINE`, `WMX3_IO_SAMPLE`, `WMX3_MOTION_HEAD`, `WMX3_MOTION_STAGE`, `CognexVisionIF`, `DiskCleaner`, `MachineDataDB`, `CPKCollector`, `MotorLoadDB`, `ScrewDriver`) with individual/batch simulation activation.
- **FR-003**: System MUST implement the `Main (Home)` dashboard with Alarm monitor boxes (`Code`, `Category`, `ALARM TIME`, `Material`), `EMO TEST` & `Ready` toggles, `MultiSequence` station map (`Hermes Input`, `Loading`, `Align`, `Process`, `Inspection`, `Unloading`, `Hermes Output`, `MPSLoading`, `MPSProcess`, plus `StationMonitors` strip with `Remove Product` context menu), `PDCA`/`Hermes`/`Vision` status LEDs, `Door Open`/`Door Lock`, `Manual Load`/`UnLoad`, mode selectors (`HERMES`, `CPK`, `PDCA`, `PDCA OFF`, `GRR`, `DRY-RUN`, `MPS Use`), `Cycle Time`/`UPH`/`UPD` cards, `Pie Ratio` chart, `Screw Torque-Angle` chart with red `NG Limit` dashed line, `Cassette` (10 rows) & `Tray` (2 columns) grids, `Latest History` list, and `Daily`/`Weekly` UPH/UPD tabs.
- **FR-004**: System MUST enforce role-based access control (`NONE`, `Operator L1`, `Line Leader L3`, `Engineer L5`, `Admin L8`) via `WinSettingsLogin` supporting simulated Badge swipe and password input, gating `Control`, `Recipe`, `Alarm`, `Monitoring`, and `Setting` behind `LEVEL5+` and supporting `ScreenLock` overlay.
- **FR-005**: System MUST execute a real-time asynchronous `MultiSequence` simulation engine moving `ProductHSG` and `ProductMPS` items across stations, computing cycle time, UPH, UPD, screw torque waveforms, PDCA inspection measurements, tossing/NG events, and motor load telemetry.
- **FR-006**: System MUST implement HIVE 3.x state management (`Running`, `Idle`, `Engineering`, `PlannedDT`, `UnplannedDT`, `Degraded`) with interactive dialogs (`WinManualDT`, `WinAutoTriggeredDT`, `WinEmergency`, `WinMaintenanceCheckList`, `WinRepairConfirm`, `WinReportEnd`), downtime minute calculation (`RepairEnd - HelpRequest`), dual HIVE server/emulator log writing, and dynamic QR code generation whenever in Down/ManualDT/Alarm state.
- **FR-007**: System MUST provide all 6 `Control` subviews: `Manual` (cylinder/vacuum/conveyor/station manual actions), `Calibration` (camera-to-stage XYTheta calibration), `Device` (per-device config & live test panel), `Sequence` (visual sequence step graph & single-sequence start/pause/stop), `WMX3 Motion Test` (servo on/home/jog/abs/rel move), and `BUSOP` (`Force`, `LoadCell`, `Vision Static/Dynamic T31` SOP verification).
- **FR-008**: System MUST provide all 3 `Recipe` subviews: `Recipe` (`MainRecipe` & `CameraRecipe` parameter editors), `Teaching` (`WMX3PositionRecipe` multi-tab card editor with `Add/Edit/Del Tab`, `Add Motion`, `Position List`, `Clone To`, `Save Tab`, `Save All`, `Reload`), and `Model` (`MetalModel` management & active model switching).
- **FR-009**: System MUST provide all 5 `Alarm` subviews: `AlarmHistory` (timestamped log with duration & reset), `Tossing List` & `NG List` (station-filtered `HSG`/`MPS` history with export), `AlarmDescription` (searchable multilingual `en/ko/zh/vi` error catalog & solutions), and `AlarmValidationReport` (audit of configured alarm codes and HIVE standard compliance).
- **FR-010**: System MUST provide all 5 `Monitoring` subviews: `WMX3 IO` (interactive 32-input / 32-output bit matrix), `FATP Cpk` (date/hour filter, `Today`/`All`, multi-item selector, capability charts with `Cp`/`Cpu`/`Cpl`/`Cpk` and `Export CSV`), `PDCADataMonitoring` (live tabular inspection stream), `PerformanceMetrics` (date picker popup, station tabs with OEE/Availability/Performance/Quality & state breakdown + `Export CSV`), and `LoadFactor` (per-axis servo torque load trend charts).
- **FR-011**: System MUST provide all 5 `Setting` subviews: `EquipSetting` (`Equipment.json` & `HiveSetting.json` editor including Site `FXCN/TEMD/THER/FXBL/FXGL` and dual HIVE Emulator logging), `SpareParts` (banded consumable table with `Unit`, bilingual `Name`, `PartNumber`, `Use Alarm`, `AlarmCode` selector, `Count`/`Warning`/`Limit`, `Reset`, `String Generator`, `Save`/`Reload`), `LogSetting` (up to 20 log configs per v2.6.5.1), `BackUp` (one-click & scheduled config snapshot archive/restore), and `Shortcut` (hotkey bindings including `Ctrl+P` full window PNG screenshot).
- **FR-012**: System MUST persist all settings, recipes, models, spare parts, alarm histories, and HIVE/PDCA logs locally in human-readable JSON/CSV files under `_data/`, pre-populated with realistic `RSMS-001` defaults.

### Key Entities

- **EquipmentConfig & HiveSetting**: Machine identity (`RSMS-001`, Site, LineNo, MachineNo, SimulationMode, SequenceMode, Language) and HIVE 3.x endpoints (Server IP/Port, Emulator IP/Port, dual-logging flag, Weekly/Monthly maintenance intervals).
- **Station & SequenceState**: Represents `HSG` and `MPS` stations and their constituent sequences (`Loading`, `Align`, `Process`, `Inspection`, `Unloading`, `MPSLoading`, `MPSProcess`), tracking active product serial/WIP, step index, and `Idle`/`Running`/`Paused`/`Stopped`/`Alarm` state.
- **HiveSession & DowntimeRecord**: Current HIVE state, active QR payload, badge operator session (`Name`, `Vendor`, `Level`), `HelpRequest` timestamp, `RepairEnd` timestamp, bilingual cause, and replaced spare part.
- **TeachingRecipe (`WMX3PositionRecipe`)**: Tabs, Motion IDs (`WMX3_MOTION_HEAD`, `WMX3_MOTION_STAGE`, `WMX3_MOTION_TEACHINGSAMPLE`), and named target coordinates (`Loading`, `Processing`, `Unloading`, `Align_Ready`, `Align`, `Align_Unload`) with Position, Velocity, Accel, Decel.
- **PdcaMeasurementItem & SpcResult**: Inspection parameter (`USL`, `LSL`, timestamped `DataPoints`) and derived `N`, `Mean`, `StdDev`, `Min`, `Max`, `Cp`, `Cpu`, `Cpl`, `Cpk`, `UPH`.
- **AlarmRecord & SparePartItem**: Multilingual alarm catalog/history entries and consumable parts tracking current cycle count against warning/limit thresholds.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of the 6 main menus and all 25 registered subviews plus 7 operational modal dialogs (`DeviceInit`, `SettingsLogin`, `Jog`, `ManualDT`, `MaintenanceCheckList`, `ReportEnd`, `ScreenLock`) are navigable and interactive with zero dead/unimplemented views.
- **SC-002**: Visual layout, color tokens (`#102236`, `#E0E7FF`, `#F0F0F0`, `#E0E4E9`, `#DDEBF1`, `#8DD471`, `#518FFF`, `#E15358`, `#FFDA5A`), and control placements on `Shell` and `Home` match `AuroraMachine2SG_v2.6.7.6` 1:1.
- **SC-003**: Starting production (`Play`) runs continuous multi-station cycles at ~1–2 seconds per simulated step, updating live KPIs, charts, trays, and logs smoothly with zero UI freezes.
- **SC-004**: `SpcCalculator` reproduces exact `Cp`, `Cpu`, `Cpl`, `Cpk`, and `UPH` values matching the reference formula across normal, single-sample, empty, and inverted-spec edge cases in 100% of unit tests.
- **SC-005**: Every configuration, recipe, teaching coordinate, and spare-part edit persists across application restarts and CSV/PNG exports produce valid files on disk.
- **SC-006**: Application compiles and passes 100% of automated tests out-of-the-box on Windows without installing commercial SDKs, databases, or license dongles.

## Assumptions

- Target operating environment is Windows 10/11 `x64` with `.NET 8 Desktop Runtime` already installed on this machine.
- Physical factory hardware (`WMX3` EtherCAT master, `AXL` motion board, `Cognex`/`Keyence` cameras, serial `ScrewDriver`, `JRK6`/`RFID` readers) and factory intranet servers (`10.0.0.2` HIVE / BALI) are simulated in-process with realistic physics/data generators and local file logs so the user can operate every feature immediately on any PC.
