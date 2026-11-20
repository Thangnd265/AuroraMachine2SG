using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AuroraMachine2SG.Core.Models;
using AuroraMachine2SG.Core.ViewModels;

namespace AuroraMachine2SG.App.Views;

public partial class HomeView : UserControl
{
    public HomeView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => RefreshCharts();
    }

    public void RefreshCharts()
    {
        if (DataContext is MainShellViewModel vm)
        {
            DailyChart.Values = vm.Runtime.HourlyCounts.ToArray();
            WeeklyChart.Values = vm.Runtime.WeeklyCounts.ToArray();
        }
    }

    private MainShellViewModel? Vm => DataContext as MainShellViewModel;

    private void DoorOpen_Click(object sender, RoutedEventArgs e) => Vm?.Runtime.TrySetDoorLocked(false);
    private void DoorLock_Click(object sender, RoutedEventArgs e) => Vm?.Runtime.TrySetDoorLocked(true);
    private void ManualLoad_Click(object sender, RoutedEventArgs e) => Vm?.Runtime.ManualLoadProduct();
    private void ManualUnload_Click(object sender, RoutedEventArgs e) { Vm?.Runtime.ManualUnloadProduct(); RefreshCharts(); }

    private void ModeRadio_Click(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton { Tag: string tag } && Enum.TryParse<MachineMode>(tag, out var mode))
            Vm?.Runtime.TryChangeMode(mode);
    }

    private void StationSlot_RightClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { Tag: StationSlot slot } fe)
        {
            var menu = new ContextMenu();
            var item = new MenuItem { Header = $"Remove Product ({slot.Position})" };
            item.Click += (_, _) => Vm?.Runtime.RemoveProductFromSlot(slot);
            menu.Items.Add(item);
            fe.ContextMenu = menu;
            menu.IsOpen = true;
        }
    }
}
