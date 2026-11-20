using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using AuroraMachine2SG.App.Controls;
using AuroraMachine2SG.Core.Models;
using AuroraMachine2SG.Core.Services;
using AuroraMachine2SG.Core.ViewModels;

namespace AuroraMachine2SG.App.Dialogs;

public static class OperationalDialogs
{
    public static void ShowDeviceInit(Window owner, MachineRuntime rt)
    {
        var win = CreateBaseDialog(owner, "Device Initialization (RSMS-001)", 680, 520);
        var root = new Grid { Margin = new Thickness(14) };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var hdr = new TextBlock
        {
            Text = "Aurora 2.x Device Manager — Activate Registered Equipment Devices",
            FontSize = 15, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 10)
        };
        root.Children.Add(hdr);

        var dg = new DataGrid
        {
            ItemsSource = rt.Devices,
            AutoGenerateColumns = false,
            CanUserAddRows = false
        };
        dg.Columns.Add(new DataGridCheckBoxColumn { Header = "Active", Binding = new System.Windows.Data.Binding("IsActivated"), Width = 60 });
        dg.Columns.Add(new DataGridTextColumn { Header = "Device ID", Binding = new System.Windows.Data.Binding("DeviceId"), Width = 170, IsReadOnly = true });
        dg.Columns.Add(new DataGridTextColumn { Header = "Category", Binding = new System.Windows.Data.Binding("Category"), Width = 100, IsReadOnly = true });
        dg.Columns.Add(new DataGridTextColumn { Header = "Endpoint / Port", Binding = new System.Windows.Data.Binding("EndpointOrPort"), Width = 150, IsReadOnly = true });
        dg.Columns.Add(new DataGridTextColumn { Header = "Status", Binding = new System.Windows.Data.Binding("StatusMessage"), Width = new DataGridLength(1, DataGridLengthUnitType.Star), IsReadOnly = true });
        Grid.SetRow(dg, 1);
        root.Children.Add(dg);

        var footer = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 12, 0, 0) };
        var btnInitAll = MakeButton("Activate All (Simul)", "#8DD471", 150);
        btnInitAll.Click += (_, _) => { rt.InitializeAllDevices(true); dg.Items.Refresh(); };

        var btnOk = MakeButton("Confirm & Start HMI", "#518FFF", 160, Brushes.White);
        btnOk.Margin = new Thickness(8, 0, 0, 0);
        btnOk.Click += (_, _) => { win.DialogResult = true; win.Close(); };

        footer.Children.Add(btnInitAll);
        footer.Children.Add(btnOk);
        Grid.SetRow(footer, 2);
        root.Children.Add(footer);

        win.Content = root;
        win.ShowDialog();
    }

    public static bool ShowSettingsLogin(Window owner, MachineRuntime rt)
    {
        var win = CreateBaseDialog(owner, "Settings Login", 420, 560);
        var root = new StackPanel { Margin = new Thickness(16) };

        root.Children.Add(new TextBlock { Text = "Permission Group", FontWeight = FontWeights.Bold, FontSize = 14 });
        var cmbGroup = new ComboBox
        {
            ItemsSource = new[] { "Operator (L1)", "Line Leader (L3)", "Engineer (L5)", "Administrator (L8)" },
            SelectedIndex = 3, Height = 34, Margin = new Thickness(0, 6, 0, 12), FontSize = 14
        };
        root.Children.Add(cmbGroup);

        root.Children.Add(new TextBlock
        {
            Text = "Please swipe badge to login (Simulation Presets):",
            FontSize = 13, Foreground = Brushes.DarkSlateGray, Margin = new Thickness(0, 0, 0, 6)
        });

        var txtName = new TextBox { Text = "왕소명 (Wang Xiaoming)", Padding = new Thickness(5), Margin = new Thickness(0, 3, 0, 6) };
        var txtVendor = new TextBox { Text = "PRI", Padding = new Thickness(5), Margin = new Thickness(0, 3, 0, 6) };
        var txtLevel = new TextBox { Text = "L8", IsReadOnly = true, Padding = new Thickness(5), Margin = new Thickness(0, 3, 0, 6) };
        var pwdBox = new PasswordBox { Password = "1234", Padding = new Thickness(5), Margin = new Thickness(0, 3, 0, 8) };

        var statusBar = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(0x8D, 0xD4, 0x71)),
            Height = 22, CornerRadius = new CornerRadius(3), Margin = new Thickness(0, 6, 0, 10),
            Child = new TextBlock { Text = "Badge Reader Ready — Select Quick Swipe or Press Login", FontSize = 11, FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center }
        };

        var swipeGrid = new UniformGrid { Columns = 2, Margin = new Thickness(0, 0, 0, 10) };
        (string Label, string Name, string Vendor, string Lvl, int Idx)[] presets =
        [
            ("Swipe Operator (L1)", "Tran Van A", "FXCN", "L1", 0),
            ("Swipe Leader (L3)", "Nguyen Thi B", "FXCN", "L3", 1),
            ("Swipe Engineer (L5)", "Kim Minsoo", "LGPRI", "L5", 2),
            ("Swipe Admin (L8)", "Wang Xiaoming", "PRI", "L8", 3)
        ];
        foreach (var p in presets)
        {
            var b = MakeButton(p.Label, "#E0E7FF", 175);
            b.Margin = new Thickness(3);
            b.Click += (_, _) =>
            {
                txtName.Text = p.Name;
                txtVendor.Text = p.Vendor;
                txtLevel.Text = p.Lvl;
                cmbGroup.SelectedIndex = p.Idx;
                ((TextBlock)statusBar.Child).Text = $"Badge Swiped OK: {p.Name} ({p.Lvl})";
            };
            swipeGrid.Children.Add(b);
        }
        root.Children.Add(swipeGrid);

        root.Children.Add(LabeledRow("Name :", txtName));
        root.Children.Add(LabeledRow("Vendor :", txtVendor));
        root.Children.Add(LabeledRow("Level :", txtLevel));
        root.Children.Add(LabeledRow("Password :", pwdBox));
        root.Children.Add(statusBar);

        var btnRow = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 10, 0, 0) };
        var btnLogin = MakeButton(rt.Loc["Login"], "#518FFF", 140, Brushes.White);
        btnLogin.Click += (_, _) =>
        {
            var level = cmbGroup.SelectedIndex switch
            {
                0 => PermissionLevel.Operator,
                1 => PermissionLevel.LineLeader,
                2 => PermissionLevel.Engineer,
                _ => PermissionLevel.Admin
            };
            rt.Hive.SwipeBadge($"BDG-{cmbGroup.SelectedIndex + 1:000}", txtName.Text, txtVendor.Text, level);
            win.DialogResult = true;
            win.Close();
        };

        var btnLogout = MakeButton(rt.Loc["Logout"], "#EAEAEB", 110);
        btnLogout.Margin = new Thickness(8, 0, 0, 0);
        btnLogout.Click += (_, _) => { rt.Hive.Logout(); win.DialogResult = false; win.Close(); };

        btnRow.Children.Add(btnLogin);
        btnRow.Children.Add(btnLogout);
        root.Children.Add(btnRow);

        win.Content = root;
        return win.ShowDialog() == true;
    }

    public static void ShowHiveDowntimeAndMaintenance(Window owner, MachineRuntime rt)
    {
        var win = CreateBaseDialog(owner, "HIVE 3.x Downtime / Maintenance / Report End", 720, 560);
        var tabs = new TabControl { Margin = new Thickness(10) };

        // Tab 1: Manual DT & Report End (with bilingual causes & red-bordered Spare-Part selector per v2.6.6.0)
        var dtPanel = new Grid { Margin = new Thickness(12) };
        dtPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
        dtPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var leftStack = new StackPanel { Margin = new Thickness(0, 0, 12, 0) };
        leftStack.Children.Add(new TextBlock { Text = $"Current HIVE State: {rt.Hive.CurrentState}   |   Active Alarm: {rt.ActiveAlarmCode}", FontWeight = FontWeights.Bold, FontSize = 14, Margin = new Thickness(0, 0, 0, 10) });

        leftStack.Children.Add(new TextBlock { Text = "Downtime Cause (Bilingual EN;ZH Mandatory per v2.6.6.0):", FontWeight = FontWeights.SemiBold });
        var cmbCause = new ComboBox
        {
            ItemsSource = new[]
            {
                "Preventive Maintenance;设备保养",
                "Material Replacement;物料更换",
                "Vision Calibration;视觉标定",
                "Screw Bit Replacement;电批批头更换",
                "Hermes Line Starved;上下游待料"
            },
            SelectedIndex = 0, Height = 32, Margin = new Thickness(0, 5, 0, 12)
        };
        leftStack.Children.Add(cmbCause);

        leftStack.Children.Add(new TextBlock { Text = "Replaced Consumable / Spare-Part (Red Border Highlight per Foxconn Spec):", FontWeight = FontWeights.SemiBold });
        var cmbPart = new ComboBox
        {
            ItemsSource = rt.SpareParts.Select(s => s.Name).Prepend("none;无更换").ToList(),
            SelectedIndex = 0, Height = 32
        };
        var redPartBox = new Border
        {
            BorderBrush = Brushes.Red, BorderThickness = new Thickness(2), CornerRadius = new CornerRadius(4),
            Padding = new Thickness(4), Margin = new Thickness(0, 5, 0, 14), Child = cmbPart
        };
        leftStack.Children.Add(redPartBox);

        var lblResult = new TextBlock { Foreground = Brushes.DarkBlue, FontWeight = FontWeights.Bold, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 4, 0, 10) };
        leftStack.Children.Add(lblResult);

        var actionWrap = new WrapPanel();
        var btnEnterDt = MakeButton("Trigger Manual DT", "#FFDA5A", 155);
        btnEnterDt.Margin = new Thickness(0, 0, 8, 8);
        btnEnterDt.Click += (_, _) =>
        {
            rt.StopProduction(cmbCause.SelectedItem?.ToString() ?? "Preventive Maintenance;设备保养");
            lblResult.Text = $"Entered PlannedDT. QR generated: {rt.Hive.ActiveQrPayload}";
        };

        var btnDegraded = MakeButton("Set Degraded Mode", "#D6D6D6", 155);
        btnDegraded.Margin = new Thickness(0, 0, 8, 8);
        btnDegraded.Click += (_, _) =>
        {
            rt.Hive.SetDegraded("MPSProcess Bypass");
            lblResult.Text = "Switched to Degraded Mode (Single-station bypass).";
        };

        var btnReportEnd = MakeButton("Submit Report End & Resume Idle", "#8DD471", 240);
        btnReportEnd.Margin = new Thickness(0, 0, 8, 8);
        btnReportEnd.Click += (_, _) =>
        {
            double mins = rt.ClearActiveAlarmAndResume(
                cmbCause.SelectedItem?.ToString() ?? "Preventive Maintenance;设备保养",
                cmbPart.SelectedItem?.ToString() ?? "none;无更换");
            lblResult.Text = $"Report End submitted! Downtime = {mins:F2} min. Machine returned to IDLE.";
        };

        actionWrap.Children.Add(btnEnterDt);
        actionWrap.Children.Add(btnDegraded);
        actionWrap.Children.Add(btnReportEnd);
        leftStack.Children.Add(actionWrap);
        dtPanel.Children.Add(leftStack);

        var qrSide = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
        qrSide.Children.Add(new TextBlock { Text = "HIVE 3.x QR Code", FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 6) });
        var qrCtrl = new QrCodeView { Width = 170, Height = 170 };
        qrCtrl.SetBinding(QrCodeView.PayloadProperty, new System.Windows.Data.Binding("Hive.ActiveQrPayload") { Source = rt });
        qrSide.Children.Add(qrCtrl);
        var qrTxt = new TextBlock { FontSize = 10, Foreground = Brushes.DimGray, TextWrapping = TextWrapping.Wrap, Width = 180, Margin = new Thickness(0, 6, 0, 0) };
        qrTxt.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding("Hive.ActiveQrPayload") { Source = rt });
        qrSide.Children.Add(qrTxt);
        Grid.SetColumn(qrSide, 1);
        dtPanel.Children.Add(qrSide);

        tabs.Items.Add(new TabItem { Header = "Manual DT / Report End", Content = dtPanel });

        // Tab 2: Maintenance CheckList (v2.6.6.0 rule)
        var chkStack = new StackPanel { Margin = new Thickness(14) };
        chkStack.Children.Add(new TextBlock
        {
            Text = "PM Maintenance CheckList (Submit always allowed; Counter resets ONLY when Badge logged-in & 100% checked):",
            TextWrapping = TextWrapping.Wrap, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 10)
        });

        var checkBoxes = rt.HiveCfg.MaintenanceChecklistItems
            .Select(item => new CheckBox { Content = item, IsChecked = true, FontSize = 13, Margin = new Thickness(0, 5, 0, 5) })
            .ToList();
        foreach (var cb in checkBoxes) chkStack.Children.Add(cb);

        var chkStatus = new TextBlock { FontWeight = FontWeights.Bold, Foreground = Brushes.SeaGreen, Margin = new Thickness(0, 10, 0, 10) };
        var chkBtns = new StackPanel { Orientation = Orientation.Horizontal };
        var btnWeekly = MakeButton("Submit Weekly PM", "#518FFF", 160, Brushes.White);
        btnWeekly.Click += (_, _) =>
        {
            bool reset = rt.Hive.SubmitMaintenanceChecklist(checkBoxes.Select(c => c.IsChecked == true).ToList(), isWeekly: true);
            chkStatus.Text = reset
                ? "Submitted & Weekly Counter reset to 7 D!"
                : "Submitted (Counter unchanged: requires Badge Login + all items checked).";
        };
        var btnMonthly = MakeButton("Submit Monthly PM", "#4233A6", 160, Brushes.White);
        btnMonthly.Margin = new Thickness(10, 0, 0, 0);
        btnMonthly.Click += (_, _) =>
        {
            bool reset = rt.Hive.SubmitMaintenanceChecklist(checkBoxes.Select(c => c.IsChecked == true).ToList(), isWeekly: false);
            chkStatus.Text = reset
                ? "Submitted & Monthly Counter reset to 30 D!"
                : "Submitted (Counter unchanged: requires Badge Login + all items checked).";
        };
        chkBtns.Children.Add(btnWeekly);
        chkBtns.Children.Add(btnMonthly);
        chkStack.Children.Add(chkBtns);
        chkStack.Children.Add(chkStatus);

        tabs.Items.Add(new TabItem { Header = "Maintenance CheckList", Content = chkStack });

        win.Content = tabs;
        win.ShowDialog();
    }

    public static void ShowJogWindow(Window owner, ControlViewModel ctrlVm)
    {
        var win = CreateBaseDialog(owner, "WMX3 Multi-Axis JOG Pendant", 500, 380);
        var sp = new StackPanel { Margin = new Thickness(16) };

        sp.Children.Add(new TextBlock { Text = "Select Servo Axis:", FontWeight = FontWeights.Bold });
        var cmbAxis = new ComboBox
        {
            ItemsSource = ctrlVm.Runtime.Axes,
            DisplayMemberPath = "AxisName",
            SelectedIndex = 0, Height = 34, Margin = new Thickness(0, 6, 0, 12)
        };
        sp.Children.Add(cmbAxis);

        var posTxt = new TextBlock { FontSize = 20, FontWeight = FontWeights.Bold, Foreground = Brushes.Navy, Margin = new Thickness(0, 0, 0, 12) };
        void RefreshPos()
        {
            if (cmbAxis.SelectedItem is AxisStatus ax)
                posTxt.Text = $"{ax.AxisName}   Actual: {ax.ActualPos:F3} mm   |   Load: {ax.LoadPercent:F1}%";
        }
        cmbAxis.SelectionChanged += (_, _) => RefreshPos();
        RefreshPos();
        sp.Children.Add(posTxt);

        sp.Children.Add(new TextBlock { Text = "Step Distance (mm):", FontWeight = FontWeights.SemiBold });
        var cmbStep = new ComboBox { ItemsSource = new[] { 0.1, 1.0, 5.0, 10.0, 25.0 }, SelectedIndex = 2, Height = 32, Margin = new Thickness(0, 4, 0, 16) };
        sp.Children.Add(cmbStep);

        var jogRow = new UniformGrid { Columns = 3 };
        var btnMinus = MakeButton("◀ JOG -", "#E0E7FF", 130);
        btnMinus.Height = 48;
        btnMinus.Click += (_, _) =>
        {
            if (cmbAxis.SelectedItem is AxisStatus ax && cmbStep.SelectedItem is double st)
            {
                ctrlVm.JogAxis(ax.AxisName, -st);
                RefreshPos();
            }
        };

        var btnHome = MakeButton("HOME (0.00)", "#FDFDBF", 130);
        btnHome.Height = 48;
        btnHome.Click += (_, _) =>
        {
            if (cmbAxis.SelectedItem is AxisStatus ax)
            {
                ctrlVm.HomeAxis(ax.AxisName);
                RefreshPos();
            }
        };

        var btnPlus = MakeButton("JOG + ▶", "#8DD471", 130);
        btnPlus.Height = 48;
        btnPlus.Click += (_, _) =>
        {
            if (cmbAxis.SelectedItem is AxisStatus ax && cmbStep.SelectedItem is double st)
            {
                ctrlVm.JogAxis(ax.AxisName, st);
                RefreshPos();
            }
        };

        jogRow.Children.Add(btnMinus);
        jogRow.Children.Add(btnHome);
        jogRow.Children.Add(btnPlus);
        sp.Children.Add(jogRow);

        win.Content = sp;
        win.ShowDialog();
    }

    public static string? PromptText(Window owner, string title, string label, string initial = "")
    {
        var win = CreateBaseDialog(owner, title, 380, 190);
        var sp = new StackPanel { Margin = new Thickness(16) };
        sp.Children.Add(new TextBlock { Text = label, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 8) });
        var tb = new TextBox { Text = initial, Padding = new Thickness(6), FontSize = 14 };
        sp.Children.Add(tb);
        var ok = MakeButton("OK", "#518FFF", 110, Brushes.White);
        ok.Margin = new Thickness(0, 14, 0, 0);
        ok.HorizontalAlignment = HorizontalAlignment.Right;
        ok.Click += (_, _) => { win.DialogResult = true; win.Close(); };
        sp.Children.Add(ok);
        win.Content = sp;
        return win.ShowDialog() == true ? tb.Text.Trim() : null;
    }

    private static Window CreateBaseDialog(Window owner, string title, double w, double h) => new()
    {
        Owner = owner,
        Title = title,
        Width = w,
        Height = h,
        WindowStartupLocation = WindowStartupLocation.CenterOwner,
        ResizeMode = ResizeMode.NoResize,
        Background = Brushes.White
    };

    private static Grid LabeledRow(string label, UIElement editor)
    {
        var g = new Grid { Margin = new Thickness(0, 2, 0, 2) };
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(95) });
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        g.Children.Add(new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center, FontWeight = FontWeights.SemiBold });
        Grid.SetColumn(editor, 1);
        g.Children.Add(editor);
        return g;
    }

    private static Button MakeButton(string text, string bgHex, double width, Brush? fg = null) => new()
    {
        Content = text,
        Width = width,
        Height = 34,
        Background = (SolidColorBrush)new BrushConverter().ConvertFromString(bgHex)!,
        Foreground = fg ?? Brushes.Black,
        FontWeight = FontWeights.Bold,
        BorderBrush = Brushes.SlateGray,
        BorderThickness = new Thickness(1),
        Cursor = System.Windows.Input.Cursors.Hand
    };
}
