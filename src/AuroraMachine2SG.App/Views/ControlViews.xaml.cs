using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using AuroraMachine2SG.App.Dialogs;
using AuroraMachine2SG.Core.ViewModels;

namespace AuroraMachine2SG.App.Views;

public partial class ControlViews : UserControl
{
    private readonly Random _rng = new();
    public ControlViews() => InitializeComponent();

    private MainShellViewModel? Vm => DataContext as MainShellViewModel;

    private void ManualLoad_Click(object s, RoutedEventArgs e) => Vm?.Runtime.ManualLoadProduct();
    private void ManualUnload_Click(object s, RoutedEventArgs e) => Vm?.Runtime.ManualUnloadProduct();
    private void SingleStep_Click(object s, RoutedEventArgs e)
    {
        if (Vm is null) return;
        bool wasRunning = Vm.Runtime.IsSequenceRunning;
        if (!wasRunning) Vm.Runtime.StartProduction();
        Vm.Runtime.StepSimulationCycle();
        if (!wasRunning) Vm.Runtime.PauseProduction();
    }

    private void OpenJog_Click(object s, RoutedEventArgs e)
    {
        if (Vm is not null && Window.GetWindow(this) is Window w)
            OperationalDialogs.ShowJogWindow(w, Vm.ControlVM);
    }

    private void RunCalib_Click(object s, RoutedEventArgs e) => Vm?.ControlVM.RunCalibration9Point();
    private void SeqStart_Click(object s, RoutedEventArgs e) => Vm?.Runtime.StartProduction();
    private void SeqPause_Click(object s, RoutedEventArgs e) => Vm?.Runtime.PauseProduction();

    private void SeqStepSelected_Click(object s, RoutedEventArgs e)
    {
        if (Vm?.ControlVM.SelectedSequence is { } seq)
        {
            seq.ClearBreakpointPause();
            seq.StepOneNode();
        }
    }

    private void SeqCycleStop_Click(object s, RoutedEventArgs e)
    {
        if (Vm is null) return;
        Vm.Runtime.SharedSequenceContext.IsCycleStopRequested = true;
        Vm.Runtime.LoggerHub.Warn("SequenceCtrl", "Cycle Stop requested — sequences will exit to Final at next cycle check.");
    }

    private void SeqReset_Click(object s, RoutedEventArgs e) => Vm?.ControlVM.SelectedSequence?.Reset();

    private void ResumeBp_Click(object s, RoutedEventArgs e)
    {
        if (Vm?.ControlVM.SelectedSequence is { } seq)
        {
            seq.ClearBreakpointPause();
            seq.StepOneNode();
        }
    }

    private void PublishEvent_Click(object s, RoutedEventArgs e)
    {
        if (Vm is null || string.IsNullOrWhiteSpace(Vm.ControlVM.EventToPublish)) return;
        string ev = Vm.ControlVM.EventToPublish.Trim();
        Vm.Runtime.SharedSequenceContext.PendingEvents.Add(ev);
        Vm.Runtime.LoggerHub.Info("EventBus", $"Published event '{ev}' into SequenceContext.");
        Vm.ControlVM.SelectedSequence?.StepOneNode();
    }

    private void ToggleManualLoadMode_Click(object s, RoutedEventArgs e)
    {
        if (Vm is null || s is not CheckBox cb) return;
        if (cb.IsChecked == true)
            Vm.Runtime.SharedSequenceContext.StateBag["ManualLoadMode"] = true;
        else
            Vm.Runtime.SharedSequenceContext.StateBag.Remove("ManualLoadMode");
    }

    private void SimSpeed_Changed(object s, SelectionChangedEventArgs e)
    {
        if (Vm is not null && s is ComboBox { SelectedItem: ComboBoxItem { Tag: string t } } &&
            double.TryParse(t, NumberStyles.Float, CultureInfo.InvariantCulture, out double mult))
        {
            Vm.Runtime.SimSpeedMultiplier = mult;
            if (Window.GetWindow(this) is MainWindow mw)
                mw.UpdateSimulationTimerSpeed();
        }
    }

    private void ExecDeviceTest_Click(object s, RoutedEventArgs e)
    {
        if (Vm?.ControlVM.SelectedDevice is { } dev)
            Vm.ControlVM.LastDeviceResponse = Vm.Runtime.ExecuteSimulatedDeviceTest(dev, Vm.ControlVM.DeviceTestCmd, Vm.ControlVM.DeviceCustomArg, Vm.ControlVM.DeviceForceNg);
    }

    private void ImportAuroraData_Click(object s, RoutedEventArgs e)
    {
        if (Vm is null || Window.GetWindow(this) is not Window w) return;
        string defaultRef = @"E:\CTQ\LGPRICTQ\AuroraMacine2SG_v2.6.7.6\Doc\Data";
        var path = OperationalDialogs.PromptText(w, "Import Aurora Project Data Folder", "Enter Aurora Doc\\Data or project config root folder:", defaultRef);
        if (!string.IsNullOrWhiteSpace(path))
        {
            int count = Vm.Runtime.ImportExternalConfigFolder(path);
            Vm.StatusBanner = count > 0
                ? $"Imported {count} configuration files from '{path}'."
                : $"No _config_* files found at '{path}'.";
        }
    }

    private void JogMinus_Click(object s, RoutedEventArgs e)
    {
        if (Vm?.ControlVM.SelectedAxis is { } ax)
            Vm.ControlVM.JogAxis(ax.AxisName, -Vm.ControlVM.JogStepMm);
    }

    private void JogHome_Click(object s, RoutedEventArgs e)
    {
        if (Vm?.ControlVM.SelectedAxis is { } ax)
            Vm.ControlVM.HomeAxis(ax.AxisName);
    }

    private void JogPlus_Click(object s, RoutedEventArgs e)
    {
        if (Vm?.ControlVM.SelectedAxis is { } ax)
            Vm.ControlVM.JogAxis(ax.AxisName, Vm.ControlVM.JogStepMm);
    }

    private void RunAllBusop_Click(object s, RoutedEventArgs e)
    {
        if (Vm is null) return;
        foreach (var item in Vm.Runtime.BusopItems)
        {
            double mid = (item.Lsl + item.Usl) / 2.0;
            double sim = mid + (_rng.NextDouble() - 0.5) * (item.Usl - item.Lsl) * 0.45;
            Vm.ControlVM.RunBusopCheck(item, sim);
        }
    }
}
