# Data Model: AuroraMachine2SG v2.6.7.6 Clone

## 1. Core Enumerations
- `MachineSite`: `FXCN`, `TEMD`, `THER`, `FXBL`, `FXGL`
- `MachineMode`: `PDCA_ONLINE`, `PDCA_OFFLINE`, `CPK`, `GRR`, `DRYRUN`
- `HiveState`: `Idle` (`#518FFF`), `Running` (`#8DD471`), `Engineering` (`#4233A6`), `PlannedDT` (`#FFDA5A`), `UnplannedDT` (`#E15358`), `Degraded` (`#D6D6D6`)
- `PermissionLevel`: `NONE = 0`, `Operator = 1`, `LineLeader = 3`, `Engineer = 5`, `Admin = 8`
- `WorkingPos`: `Loading`, `Align`, `Process`, `Inspection`, `Unloading`, `MPSLoading`, `MPSProcess`

## 2. Entities & Validation Rules

### `EquipmentConfig` (`_data/_config_setting/Equipment.json`)
- `EquipmentName` (string, required, default `"RSMS-001"`)
- `Site` (`MachineSite`, default `FXCN`)
- `LineNo`, `MachineNo` (string, non-empty)
- `SimulationMode` (bool, default `true`)
- `Language` (string, enum: `"en-US"`, `"ko-KR"`, `"zh-CN"`, `"hi-IN"`)
- `UseScreenLock` (bool, default `false`)
- `UseVirtualKeyboard` (bool, default `false`)

### `HiveConfig` (`_data/_config_setting/HiveSetting.json`)
- `ServerIp` (`"10.0.0.2"`), `ServerPort` (`5008`)
- `EmulatorIp` (`"127.0.0.1"`), `EmulatorPort` (`8080`)
- `UseDualEmulatorLogging` (bool, default `true` per v2.6.6.1)
- `HiveLogPath` (`"_data/HIVE/log"`), `EmulatorLogPath` (`"_data/HIVE/emulator_log"`)
- `WeeklyMaintRemainDays` (int, `0..7`, default `5`), `MonthlyMaintRemainDays` (int, `0..30`, default `22`)

### `StationRuntime` & `ProductUnit`
- `StationNo` (`0 = HSG`, `1 = MPS`), `StationName` (`"HSGProcess"`, `"MPSProcess"`)
- `Positions`: ordered `StationSlot` items (`Position`, `HasProduct`, `SerialNo`, `WipCode`, `StepStatus`, `LastTorqueNm`, `IsPass`)
- State transition: `Empty -> Loaded -> Aligned -> Processed -> Inspected -> Unloaded` (increments `UPD`, recalculates `CycleTime` and `UPH`, appends `LatestHistory` and `PDCADataItem`).

### `TeachingRecipe` (`_data/_config_recipe/WMX3PositionRecipe.json`)
- `TabNames` (`List<string>`: `"Stage_Main"`, `"Head_Assy"`, `"MPS_Sub"`)
- `MotionIds` (`"WMX3_MOTION_STAGE"`, `"WMX3_MOTION_HEAD"`, `"WMX3_MOTION_TEACHINGSAMPLE"`)
- `Cards`: per-tab motion cards containing 6 standard positions (`Loading`, `Processing`, `Unloading`, `Align_Ready`, `Align`, `Align_Unload`) with `TargetPos`, `Velocity`, `Accel`, `Decel`, `InPosWidth`.

### `PdcaMeasurementItem` & `SpcResult`
- `Name`, `Unit`, `LSL`, `USL`, `IsShown`, `DataPoints` (`Timestamp`, `Value`, `SerialNo`)
- `SpcResult`: `N`, `Mean`, `StdDev`, `Min`, `Max`, `Cp`, `Cpu`, `Cpl`, `Cpk`, `StartTime`, `EndTime`, `UPH` (exact formula from `SGComponent.CPK.SpcCalculator`).

### `SparePartItem` (`_data/_config_setting/SpareParts.json`)
- `Unit`, `Name` (bilingual format `"english;中文"`, e.g. `"camera;相机"`, `"suction_nozzle;吸嘴"`, `"screw_bit;批头"`), `PartNumber`, `IsUseAlarm`, `AlarmCode`, `CurrentCount`, `WarningLimit`, `MaxLimit`.
- Rule: When `IsUseAlarm && CurrentCount >= MaxLimit`, raise alarm `AlarmCode`.

### `AlarmCatalogEntry` & `AlarmHistoryRecord`
- `Code` (int), `Category` (`"Heavy"` / `"Light"` / `"Maintenance"`), `Unit`, `DescEn`, `DescKo`, `DescZh`, `SolutionEn`, `SolutionKo`, `SolutionZh`.
- `DowntimeMinutes = (RepairEnd - HelpRequest).TotalMinutes` (rounded to 2 decimals, >= 0).
