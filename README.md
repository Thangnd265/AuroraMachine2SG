# AuroraMachine2SG (v2.6.7.6 Standalone 1:1 WPF Clone)

Standalone `.NET 8` Windows WPF desktop reconstruction of **LG PRI Aurora 2.x SG Standard Equipment Controller (`RSMS-001` v2.6.7.6)**, built with zero proprietary dependencies (`Aurora.exe`, `DevExpress`, `PriLicenseCheckerLib`, or hardware vendor DLLs not required).

## Quick Run

```powershell
# Build & Run Unit Tests
dotnet build AuroraMachine2SG.sln
dotnet run --project tests\AuroraMachine2SG.Tests

# Start the Equipment HMI Application
dotnet run --project src\AuroraMachine2SG.App
```

## Included Subsystems (6 Main Menus / 25 Subviews / 5 Dialogs)
- **Top Shell & HIVE 3.x Bar**: `Play` / `Pause` / `Stop` (`UCHiveIndicator`), `Open Folder`, `JOG` Multi-Axis Pendant, `Weekly`/`Monthly` PM Counter + live 21×21 QR Code, Active Device pills, 4-language switcher (`en-US`, `ko-KR`, `zh-CN`, `hi-IN`), `ScreenLock` safety overlay, and Badge/Password RBAC Login (`L1/L3/L5/L8`).
- **Main (`Home`)**: Alarm banner (`Code`, `Category`, `ALARM TIME`, `Material`, `EMO TEST`, `Ready`), `MultiSequence` station map (`HSG` 5-stage + `MPS` 2-stage + right-click `Remove Product` tossing), Door Open/Lock & Mode interlocks (`CPK`, `PDCA`, `PDCA OFF`, `GRR`, `DRY-RUN`, `HERMES`, `MPS Use`), `Cycle Time`/`UPH`/`UPD`, Yield Pie Chart, Gantry Screw Torque-Angle Curve, Cassette/Tray matrix, Latest History, and Daily/Weekly UPH/UPD charts.
- **Control**: `Manual`, `Calibration` (9-point XYTheta), `Device` (20 simulated devices), `Sequence`, `WMX3 Motion Test`, `BUSOP` (`Force`, `LoadCell`, `Vision T31`).
- **Recipe**: `MainRecipe` & `CameraRecipe`, `Teaching` (`WMX3PositionRecipe` multi-tab card editor with `Clone Card To Tab`), `Model` (`MetalModel`).
- **Alarm**: `AlarmHistory`, `Tossing List`, `NG List`, `AlarmDescription` (EN/KO/ZH search & solutions), `AlarmValidationReport`.
- **Monitoring**: `WMX3 IO` (16-In / 16-Out matrix), `FATP Cpk` (`SpcCalculator` `Cp`/`Cpk`/`USL`/`LSL` charts + CSV export), `PDCADataMonitoring`, `PerformanceMetrics` (OEE + CSV export), `LoadFactor`.
- **Setting**: `EquipSetting` + dual HIVE Server/Emulator logging, `SpareParts` (bilingual `EN;ZH`, threshold alarms, `String Generator`), `LogSetting` (20-slot cap), `BackUp` (ZIP), `Shortcut` (`Ctrl+P` PNG screenshot).
