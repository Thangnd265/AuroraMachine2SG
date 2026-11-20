using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;
using AuroraMachine2SG.App.Dialogs;
using AuroraMachine2SG.Core.Models;
using AuroraMachine2SG.Core.Sdk;
using AuroraMachine2SG.Core.Services;
using AuroraMachine2SG.Core.ViewModels;

namespace AuroraMachine2SG.App;

public partial class MainWindow : Window
{
    private readonly MainShellViewModel _vm;
    private readonly DispatcherTimer _simTimer;
    private readonly bool _skipStartupDialogs;

    public MainWindow(MainShellViewModel vm, bool skipStartupDialogs = false)
    {
        InitializeComponent();
        _vm = vm;
        _skipStartupDialogs = skipStartupDialogs;
        DataContext = _vm;

        LangCombo.ItemsSource = LocalizationService.SupportedLanguages;
        LangCombo.SelectedItem = _vm.Runtime.Loc.CurrentLanguage;

        _vm.LoginRequired += (targetMain, targetSub) =>
        {
            if (OperationalDialogs.ShowSettingsLogin(this, _vm.Runtime))
                _vm.TryNavigate(targetMain, targetSub);
        };

        _simTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(850) };
        _simTimer.Tick += (_, _) =>
        {
            ClockText.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            if (_vm.Runtime.IsSequenceRunning)
            {
                _vm.Runtime.StepSimulationCycle();
                HomeViewHost.RefreshCharts();
            }
        };
        _simTimer.Start();
        ClockText.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        Loaded += MainWindow_Loaded;
    }

    public void UpdateSimulationTimerSpeed()
    {
        double ms = Math.Clamp(850.0 / _vm.Runtime.SimSpeedMultiplier, 120.0, 3200.0);
        _simTimer.Interval = TimeSpan.FromMilliseconds(ms);
        _vm.StatusBanner = $"Simulation Speed set to {_vm.Runtime.SimSpeedMultiplier:F2}x ({ms:F0} ms/tick).";
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        HomeViewHost.RefreshCharts();
        if (!_skipStartupDialogs)
            OperationalDialogs.ShowDeviceInit(this, _vm.Runtime);
    }

    private void Play_Click(object s, RoutedEventArgs e)
    {
        if (!_vm.Runtime.StartProduction())
            _vm.StatusBanner = "Cannot Start: Ensure Safety Door is Locked and Active Alarms are cleared.";
        else
            _vm.StatusBanner = $"RUNNING [{_vm.Runtime.ActiveProfile.DisplayTitle}]";
    }

    private void Pause_Click(object s, RoutedEventArgs e)
    {
        _vm.Runtime.PauseProduction();
        _vm.StatusBanner = "Main MultiSequence PAUSED.";
    }

    private void Stop_Click(object s, RoutedEventArgs e)
    {
        _vm.Runtime.StopProduction("Manual Operator Stop;操作员手动停机");
        _vm.StatusBanner = "Stopped -> HIVE PlannedDT QR Generated.";
    }

    private void StepAllOnce_Click(object s, RoutedEventArgs e)
    {
        foreach (var seq in _vm.Runtime.ActiveSequences)
            seq.StepOneNode();
    }

    private void JumpToFlowchart_Click(object s, RoutedEventArgs e)
    {
        if (_vm.Runtime.Hive.CurrentPermission < PermissionLevel.Engineer)
            _vm.Runtime.Hive.SwipeBadge("DEV-AUTO", "Developer", "PRI", PermissionLevel.Admin);
        _vm.TryNavigate("Control", "Sequence");
    }

    private void ClearLog_Click(object s, RoutedEventArgs e) => _vm.Runtime.LoggerHub.Clear();

    private void ProfileCombo_Changed(object s, SelectionChangedEventArgs e)
    {
        _vm.ControlVM.RefreshSelectionsAfterProfileSwitch();
        _vm.StatusBanner = $"Active Profile: {_vm.Runtime.ActiveProfile.DisplayTitle}";
    }

    private void LoadPluginDll_Click(object s, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = "Select Custom IEquipmentSetup Plugin (.dll)",
            Filter = "NET 8 Machine Plugin (*.dll)|*.dll"
        };
        if (dlg.ShowDialog(this) == true)
        {
            try
            {
                var loaded = BuiltInEquipmentProfiles.LoadFromAssemblyFile(dlg.FileName);
                if (loaded.Count == 0)
                {
                    _vm.Runtime.LoggerHub.Warn("PluginLoader", $"No public IEquipmentSetup found in '{Path.GetFileName(dlg.FileName)}'.");
                    _vm.StatusBanner = "No IEquipmentSetup class found in selected DLL.";
                    return;
                }
                foreach (var p in loaded)
                    _vm.Runtime.AvailableProfiles.Add(p);
                _vm.Runtime.LoadEquipmentProfile(loaded[0]);
                _vm.ControlVM.RefreshSelectionsAfterProfileSwitch();
                _vm.StatusBanner = $"Loaded external plugin: {loaded[0].DisplayTitle}";
            }
            catch (Exception ex)
            {
                _vm.Runtime.LoggerHub.Error("PluginLoader", $"Failed loading DLL: {ex.Message}");
                _vm.StatusBanner = $"Plugin load error: {ex.Message}";
            }
        }
    }

    private void OpenFolder_Click(object s, RoutedEventArgs e)
    {
        try { Process.Start(new ProcessStartInfo { FileName = _vm.Runtime.Storage.BasePath, UseShellExecute = true }); }
        catch { }
    }

    private void Jog_Click(object s, RoutedEventArgs e) => OperationalDialogs.ShowJogWindow(this, _vm.ControlVM);
    private void HivePill_Click(object s, RoutedEventArgs e) => OperationalDialogs.ShowHiveDowntimeAndMaintenance(this, _vm.Runtime);
    private void HivePill_Click(object s, MouseButtonEventArgs e) => OperationalDialogs.ShowHiveDowntimeAndMaintenance(this, _vm.Runtime);
    private void LockScreen_Click(object s, RoutedEventArgs e) => _vm.IsScreenLocked = true;
    private void UnlockScreen_Click(object s, RoutedEventArgs e) => _vm.IsScreenLocked = false;
    private void LoginBtn_Click(object s, RoutedEventArgs e) => OperationalDialogs.ShowSettingsLogin(this, _vm.Runtime);

    private void LangCombo_SelectionChanged(object s, SelectionChangedEventArgs e)
    {
        if (LangCombo.SelectedItem is string code)
        {
            _vm.Runtime.Loc.SetLanguage(code);
            _vm.Runtime.Equipment.Language = code;
            _vm.Runtime.Storage.SaveEquipment(_vm.Runtime.Equipment);
        }
    }

    private void MainNav_Click(object s, RoutedEventArgs e)
    {
        if (s is RadioButton { Tag: string mainMenu })
            _vm.TryNavigate(mainMenu);
    }

    private void SubMenu_Click(object s, RoutedEventArgs e)
    {
        if (s is Button { Tag: string sub })
            _vm.SelectSubMenu(sub);
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.P && Keyboard.Modifiers == ModifierKeys.Control)
        {
            CaptureWindowScreenshot();
            e.Handled = true;
        }
    }

    public string CaptureWindowScreenshot(string? explicitPath = null)
    {
        UpdateLayout();
        int w = Math.Max(800, (int)ActualWidth);
        int h = Math.Max(600, (int)ActualHeight);
        var rtb = new RenderTargetBitmap(w, h, 96, 96, PixelFormats.Pbgra32);
        rtb.Render(this);

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(rtb));

        string outPath = explicitPath ?? Path.Combine(_vm.Runtime.Storage.ExportDir, $"ScreenShot_{DateTime.Now:yyyyMMdd_HHmmss}.png");
        Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
        using var fs = File.Create(outPath);
        encoder.Save(fs);

        _vm.StatusBanner = $"ScreenShot saved -> {outPath}";
        return outPath;
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        if (_vm.Runtime.IsSequenceRunning && !_skipStartupDialogs)
        {
            var res = MessageBox.Show(this,
                "If you stop the machine while machine state is running, problems may occur. Are you sure you want to quit?",
                "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (res != MessageBoxResult.Yes)
            {
                e.Cancel = true;
                return;
            }
        }
        _simTimer.Stop();
        base.OnClosing(e);
    }
}
