# Phase 0 Research: AuroraMachine2SG v2.6.7.6 1:1 Standalone Clone

## Decision 1: Target Runtime & UI Framework
- **Decision**: `.NET 8` (`net8.0` for Core/Tests, `net8.0-windows` with `UseWPF=true` for App) with zero third-party NuGet dependencies.
- **Rationale**: `Microsoft.WindowsDesktop.App 8.0.31` is pre-installed locally (`win-x64`), compiles in seconds offline without NuGet restore risk, and natively supports exact XAML layouts (`ResourceDictionary`, `DataGrid`, `UniformGrid`, `TabControl`, `Popup`, `Calendar`, `RenderTargetBitmap`, `System.IO.Compression.ZipFile`, `System.Text.Json`).
- **Alternatives considered**:
  - `.NET Framework 4.8.1` + `Aurora.exe` + `DevExpress v24.2`: Rejected because `Aurora.exe` is obfuscated/licensed (`PriLicenseCheckerLib`) and `DevExpress` requires a paid commercial license.
  - Web/Electron: Rejected because the original software is a native Windows WPF application and WPF delivers exact 1:1 XAML control fidelity (`StGroupBox20..24`, `DataGrid`, modal dialogs, `Ctrl+P` window capture).

## Decision 2: Charting, QR Code, and Backup without External Packages (Ponytail Stdlib First)
- **Decision**: Build lightweight native WPF vector controls (`PieRatioChartControl`, `TorqueCurveControl`, `BarTrendChartControl`, `CpkCapabilityChartControl`) + pure C# QR matrix renderer (`QrMatrixGenerator` -> WPF `DrawingImage`) + `System.IO.Compression.ZipFile` for AutoBackup + `RenderTargetBitmap`/`PngBitmapEncoder` for `Ctrl+P` screenshots.
- **Rationale**: Eliminates `LiveCharts`, `OxyPlot`, `DevExpress.Xpf.Charts`, and `ZXing` binary dependencies while giving crisp DPI-aware vector rendering and 100% deterministic unit testing.
- **Alternatives considered**: Pulling `OxyPlot.Wpf` / `ZXing.Net` from NuGet — rejected per Ponytail Rung 3 & 4 (native platform + stdlib covers 100% of required charts and bitmaps).

## Decision 3: Headless Testability & TDD Runner
- **Decision**: Place all domain models, engines (`SpcCalculator`, `HiveStateMachine`, `MultiSequenceEngine`, `SparePartsService`, `LocalizationService`, `StorageService`), and ViewModels in `src/AuroraMachine2SG.Core` (`net8.0`), verified by a zero-dependency deterministic test suite in `tests/AuroraMachine2SG.Tests` (`dotnet run --project tests/AuroraMachine2SG.Tests`).
- **Rationale**: Works with zero external test runner downloads, executes in <1 second per Red-Green cycle, and exits non-zero on any failed assertion.
- **Alternatives considered**: `xunit` + `Microsoft.NET.Test.Sdk` — requires downloading ~25 NuGet packages into an empty `.nuget` cache; a 40-line reflection `[Fact]` runner in `AuroraMachine2SG.Tests` achieves identical `[Fact]` discovery, clear Red/Green output, and zero external dependencies.
