# UI Navigation & Operational Contract: AuroraMachine2SG

## 1. Shell Regions
1. **Top Title & HIVE Bar (`Height=72`, `#102236`)**:
   - Brand & Machine Title (`AURORA 2.6.7.6 | RSMS-001`)
   - `UCHiveIndicator`: `Play` (`#8DD471`), `Pause` (`#518FFF`), `Stop` (`#E15358`)
   - Quick Actions: `Open Folder` (`_data` explorer), `JOG` (`WinJog` modal, disabled while running), `Lock` (`ScreenLock` overlay)
   - `UCMaintCounter`: `Weekly : X D` / `Monthly : Y D` + live `QrCodeImage` (clickable to inspect/trigger HIVE Manual DT / Maintenance)
   - Right Cluster: Active Device status pills (`WMX3_ENGINE`, `AXTLibrary`, `BCR`, `HermesUpstream`), Language selector (`en-US`, `ko-KR`, `zh-CN`, `hi-IN`), User Login badge (`Login` / `Logout [Role]`)
2. **Main Menu & Submenu Navigation (`EquipmentSetup.RegisterViews` exact mapping)**:
   - `Main` (`PermissionLevel.NONE`, `showSubMenuBar: false`) -> `Home` (`UCHomeView`)
   - `Control` (`LEVEL5`) -> `Manual` | `Calibration` | `Device` | `Sequence` | `WMX3 Motion Test` | `BUSOP`
   - `Recipe` (`LEVEL5`) -> `Recipe` | `Teaching` | `Model`
   - `Alarm` (`LEVEL5`) -> `AlarmHistory` | `Tossing List` | `NG List` | `AlarmDescription` | `AlarmValidationReport`
   - `Monitoring` (`LEVEL5`) -> `WMX3 IO` | `FATP Cpk` | `PDCADataMonitoring` | `PerformanceMetrics` | `LoadFactor`
   - `Setting` (`LEVEL5`) -> `EquipSetting` | `SpareParts` | `LogSetting` | `BackUp` | `Shortcut`
3. **Bottom Status Bar (`Height=32`, `#EAEAEB`)**:
   - Station HIVE Status Pills (`HSGProcess : Running`, `MPSProcess : Running` — click opens HIVE state transition / Manual DT / Report End dialog)
   - Current Mode & Simulation badge + `Virtual Keyboard : ON/OFF` toggle

## 2. Modal Dialogs
- `WinDeviceInit`: Startup batch/individual device activation progress dialog.
- `WinSettingsLogin`: Permission Group selector + simulated Badge Swipe (`Operator L1`, `Line Leader L3`, `Engineer L5`, `Admin L8`) + Password check + green/red swipe bar.
- `WinManualDT` / `WinMaintenanceCheckList` / `WinReportEnd`: HIVE 3.x downtime cause (`Preventive Maintenance;设备保养`, etc.), spare-part selection (red border highlight per v2.6.6.0), checklist verification, QR display, and downtime minute calculation.
- `WinJog`: Live multi-axis Jog+/Jog-/Home/ServoOn popup.
- `ScreenLock`: Full-window safety lock overlay requiring unlock confirmation.
