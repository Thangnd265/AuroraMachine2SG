using System.Windows;
using System.Windows.Controls;
using AuroraMachine2SG.Core.Models;
using AuroraMachine2SG.Core.ViewModels;

namespace AuroraMachine2SG.App.Views;

public partial class SettingViews : UserControl
{
    public SettingViews() => InitializeComponent();
    private MainShellViewModel? Vm => DataContext as MainShellViewModel;

    private void SaveEquip_Click(object s, RoutedEventArgs e)
    {
        if (Vm is null) return;
        Vm.SettingVM.SaveAllSettings();
        Vm.StatusBanner = "Saved Equipment.json, HiveSetting.json & SpareParts.json (reported parameter update to HIVE).";
    }

    private void GenStrings_Click(object s, RoutedEventArgs e)
    {
        if (Vm is not null)
            TxtGeneratedCode.Text = Vm.SettingVM.GenerateSparePartNamesCode();
    }

    private void AddPart_Click(object s, RoutedEventArgs e)
    {
        Vm?.Runtime.SpareParts.Add(new SparePartItem
        {
            Unit = "CustomUnit",
            Name = "new_part;新备件",
            PartNumber = $"SP-NEW-{Vm.Runtime.SpareParts.Count + 1:00}",
            AlarmCode = 4110 + Vm.Runtime.SpareParts.Count
        });
    }

    private void ResetPart_Click(object s, RoutedEventArgs e)
    {
        if (SpareGrid.SelectedItem is SparePartItem sp)
        {
            sp.CurrentCount = 0;
            Vm?.SettingVM.SaveAllSettings();
        }
    }

    private void AddLogger_Click(object s, RoutedEventArgs e)
    {
        if (Vm is null) return;
        bool ok = Vm.SettingVM.TryAddLogConfig($"CustomLogger_{Vm.Runtime.LogConfigs.Count + 1}", "DEBUG");
        Vm.StatusBanner = ok ? "Added logger configuration." : "Maximum 20 log configs reached (v2.6.5.1 limit).";
    }

    private void BackupNow_Click(object s, RoutedEventArgs e)
    {
        if (Vm is null) return;
        var zip = Vm.SettingVM.RunBackupNow();
        Vm.StatusBanner = $"Backup archive saved -> {zip}";
    }

    private void CaptureNow_Click(object s, RoutedEventArgs e)
    {
        if (Window.GetWindow(this) is MainWindow mw)
            mw.CaptureWindowScreenshot();
    }
}
