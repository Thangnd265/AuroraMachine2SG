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
    private void SeqStop_Click(object s, RoutedEventArgs e) => Vm?.Runtime.StopProduction();

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
