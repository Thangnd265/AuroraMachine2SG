using System.Windows;
using System.Windows.Controls;
using AuroraMachine2SG.Core.ViewModels;

namespace AuroraMachine2SG.App.Views;

public partial class MonitoringViews : UserControl
{
    public MonitoringViews() => InitializeComponent();

    private MainShellViewModel? Vm => DataContext as MainShellViewModel;

    private void CpkToday_Click(object s, RoutedEventArgs e)
    {
        if (Vm is null) return;
        Vm.MonitoringVM.StartDay = DateTime.Today;
        Vm.MonitoringVM.EndDay = DateTime.Today.AddDays(1);
        CpkApply_Click(s, e);
    }

    private void CpkAll_Click(object s, RoutedEventArgs e)
    {
        if (Vm is null) return;
        Vm.MonitoringVM.StartDay = DateTime.Today.AddDays(-30);
        Vm.MonitoringVM.EndDay = DateTime.Today.AddDays(1);
        CpkApply_Click(s, e);
    }

    private void CpkApply_Click(object s, RoutedEventArgs e)
    {
        if (Vm is null) return;
        var from = Vm.MonitoringVM.StartDay.Date.AddHours(Vm.MonitoringVM.FromHour);
        var to = Vm.MonitoringVM.EndDay.Date.AddHours(Vm.MonitoringVM.ToHour).AddMinutes(59);
        Vm.MonitoringVM.ApplySelectedItems(from, to);
    }

    private void CpkExport_Click(object s, RoutedEventArgs e)
    {
        if (Vm is null) return;
        var from = Vm.MonitoringVM.StartDay.Date.AddHours(Vm.MonitoringVM.FromHour);
        var to = Vm.MonitoringVM.EndDay.Date.AddHours(Vm.MonitoringVM.ToHour).AddMinutes(59);
        var path = Vm.MonitoringVM.ExportCpkCsv(from, to);
        Vm.StatusBanner = $"Exported FATP Cpk CSV -> {path}";
    }

    private void PerfExport_Click(object s, RoutedEventArgs e)
    {
        if (Vm is null) return;
        var path = Vm.MonitoringVM.ExportPerformanceCsv();
        Vm.StatusBanner = $"Exported Performance Metrics CSV -> {path}";
    }
}
