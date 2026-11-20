using System.Windows;
using System.Windows.Controls;
using AuroraMachine2SG.App.Dialogs;
using AuroraMachine2SG.Core.ViewModels;

namespace AuroraMachine2SG.App.Views;

public partial class AlarmViews : UserControl
{
    public AlarmViews() => InitializeComponent();
    private MainShellViewModel? Vm => DataContext as MainShellViewModel;

    private void Trig2001_Click(object s, RoutedEventArgs e) => Vm?.Runtime.RaiseAlarm(2001, true);
    private void Trig3050_Click(object s, RoutedEventArgs e) => Vm?.Runtime.RaiseAlarm(3050, true);
    private void Revalidate_Click(object s, RoutedEventArgs e) => Vm?.AlarmVM.ValidateAlarmStandard();

    private void OpenReset_Click(object s, RoutedEventArgs e)
    {
        if (Vm is not null && Window.GetWindow(this) is Window w)
            OperationalDialogs.ShowHiveDowntimeAndMaintenance(w, Vm.Runtime);
    }
}
