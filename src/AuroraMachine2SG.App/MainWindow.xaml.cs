using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using AuroraMachine2SG.App.Dialogs;
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
            SyncMainMenuRadio();
        };

        _simTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(900) };
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
            _vm.StatusBanner = "Main MultiSequence RUNNING (HSG + MPS Stations Active).";
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

    private void OpenFolder_Click(object s, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo { FileName = _vm.Runtime.Storage.BasePath, UseShellExecute = true });
        }
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
        {
            _vm.TryNavigate(mainMenu);
            SyncMainMenuRadio();
        }
    }

    private void SubMenu_Click(object s, RoutedEventArgs e)
    {
        if (s is Button { Tag: string sub })
            _vm.SelectSubMenu(sub);
    }

    private void SyncMainMenuRadio()
    {
        NavMain.IsChecked = _vm.ActiveMainMenu == "Main";
        NavControl.IsChecked = _vm.ActiveMainMenu == "Control";
        NavRecipe.IsChecked = _vm.ActiveMainMenu == "Recipe";
        NavAlarm.IsChecked = _vm.ActiveMainMenu == "Alarm";
        NavMonitoring.IsChecked = _vm.ActiveMainMenu == "Monitoring";
        NavSetting.IsChecked = _vm.ActiveMainMenu == "Setting";
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
