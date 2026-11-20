# Quickstart & Validation Guide: AuroraMachine2SG

## Prerequisites
- Windows 10/11 `x64`
- `.NET 8 SDK` (`8.0.x` with `Microsoft.WindowsDesktop.App`)

## Build & Run Automated Tests
```powershell
Set-Location C:\Users\Thang.PC\.gemini\antigravity\scratch\AuroraMachine2SG
dotnet build AuroraMachine2SG.sln
dotnet run --project tests\AuroraMachine2SG.Tests
```
Expected outcome: Build succeeds with `0 Error(s)` and test runner prints `ALL TESTS PASSED` with exit code `0`.

## Launch the Application
```powershell
dotnet run --project src\AuroraMachine2SG.App
```

## End-to-End Verification Scenarios
1. **Startup & Home**: Confirm `Device Initialization` dialog activates all 19 registered `RSMS-001` devices, then `Home` displays Alarm monitors, `MultiSequence` stations, Door controls, Mode switches, KPI cards, Screw Torque chart, Cassette/Tray grids, and Daily/Weekly charts.
2. **Production Run & Interlocks**: Click `Play` -> verify HIVE turns green (`Running`), `Door Open` disables, products advance through stations, and `UPH`/`UPD`/`Cycle Time`/charts update live. Click `Stop` -> complete HIVE `Manual DT` / `Report End` with QR code.
3. **Badge Login & Engineering Menus**: Click `Control` -> `Settings Login` appears -> click `Swipe Badge (Admin L8)` -> `Login` -> navigate all subtabs under `Control`, `Recipe` (`Teaching` save/clone), `Alarm`, `Monitoring` (`FATP Cpk` calculation + `Export CSV`), and `Setting` (`SpareParts`, `BackUp`, `Ctrl+P` screenshot).
