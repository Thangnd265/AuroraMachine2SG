using System.Collections.ObjectModel;
using System.Text;
using AuroraMachine2SG.Core.Models;
using AuroraMachine2SG.Core.Services;

namespace AuroraMachine2SG.Core.ViewModels;

public sealed class MainShellViewModel : ObservableObject
{
    public static readonly Dictionary<string, string[]> MenuTree = new()
    {
        ["Main"] = ["Main"],
        ["Control"] = ["Manual", "Calibration", "Device", "Sequence", "WMX3 Motion Test", "BUSOP"],
        ["Recipe"] = ["Recipe", "Teaching", "Model"],
        ["Alarm"] = ["AlarmHistory", "Tossing List", "NG List", "AlarmDescription", "AlarmValidationReport"],
        ["Monitoring"] = ["WMX3 IO", "FATP Cpk", "PDCADataMonitoring", "PerformanceMetrics", "LoadFactor"],
        ["Setting"] = ["EquipSetting", "SpareParts", "LogSetting", "BackUp", "Shortcut"]
    };

    private string _activeMainMenu = "Main";
    private string _activeSubMenu = "Main";
    private bool _isScreenLocked;
    private string _statusBanner = "RSMS-001 Ready (Simulation Mode)";

    public MachineRuntime Runtime { get; }
    public ControlViewModel ControlVM { get; }
    public RecipeViewModel RecipeVM { get; }
    public MonitoringViewModel MonitoringVM { get; }
    public AlarmViewModel AlarmVM { get; }
    public SettingViewModel SettingVM { get; }

    public ObservableCollection<string> CurrentSubMenus { get; } = ["Main"];

    public event Action<string, string?>? LoginRequired;

    public MainShellViewModel(MachineRuntime runtime)
    {
        Runtime = runtime;
        ControlVM = new ControlViewModel(runtime);
        RecipeVM = new RecipeViewModel(runtime);
        MonitoringVM = new MonitoringViewModel(runtime);
        AlarmVM = new AlarmViewModel(runtime);
        SettingVM = new SettingViewModel(runtime);
    }

    public string ActiveMainMenu
    {
        get => _activeMainMenu;
        private set
        {
            if (SetProperty(ref _activeMainMenu, value))
                OnPropertyChanged(nameof(ShowSubMenuBar));
        }
    }

    public string ActiveSubMenu { get => _activeSubMenu; private set => SetProperty(ref _activeSubMenu, value); }
    public bool ShowSubMenuBar => ActiveMainMenu != "Main";
    public bool IsScreenLocked { get => _isScreenLocked; set => SetProperty(ref _isScreenLocked, value); }
    public string StatusBanner { get => _statusBanner; set => SetProperty(ref _statusBanner, value); }

    public bool TryNavigate(string mainMenu, string? subMenu = null)
    {
        if (!MenuTree.TryGetValue(mainMenu, out var subs)) return false;

        if (mainMenu != "Main" && Runtime.Hive.CurrentPermission < PermissionLevel.Engineer)
        {
            LoginRequired?.Invoke(mainMenu, subMenu);
            return false;
        }

        ActiveMainMenu = mainMenu;
        CurrentSubMenus.Clear();
        foreach (var s in subs) CurrentSubMenus.Add(s);
        ActiveSubMenu = subMenu is not null && subs.Contains(subMenu) ? subMenu : subs[0];
        StatusBanner = $"View: {ActiveMainMenu} > {ActiveSubMenu}";
        return true;
    }

    public void SelectSubMenu(string subMenu)
    {
        if (CurrentSubMenus.Contains(subMenu))
        {
            ActiveSubMenu = subMenu;
            StatusBanner = $"View: {ActiveMainMenu} > {ActiveSubMenu}";
        }
    }
}

public sealed class ControlViewModel : ObservableObject
{
    private AxisStatus? _selectedAxis;
    private double _jogStepMm = 5.0;
    private double _calibOffsetX = 0.012;
    private double _calibOffsetY = -0.008;
    private double _calibThetaDeg = 0.004;
    private string _calibResult = "Calibration Ready (Camera-to-Stage Affine Matrix OK)";

    public MachineRuntime Runtime { get; }

    public AxisStatus? SelectedAxis { get => _selectedAxis; set => SetProperty(ref _selectedAxis, value); }
    public double JogStepMm { get => _jogStepMm; set => SetProperty(ref _jogStepMm, value); }
    public double CalibOffsetX { get => _calibOffsetX; set => SetProperty(ref _calibOffsetX, value); }
    public double CalibOffsetY { get => _calibOffsetY; set => SetProperty(ref _calibOffsetY, value); }
    public double CalibThetaDeg { get => _calibThetaDeg; set => SetProperty(ref _calibThetaDeg, value); }
    public string CalibResult { get => _calibResult; set => SetProperty(ref _calibResult, value); }

    public ControlViewModel(MachineRuntime runtime)
    {
        Runtime = runtime;
        _selectedAxis = runtime.Axes.FirstOrDefault();
    }

    public void JogAxis(string axisName, double deltaMm)
    {
        if (Runtime.IsSequenceRunning) return;
        var axis = Runtime.Axes.FirstOrDefault(a => a.AxisName == axisName);
        if (axis is null || !axis.ServoOn) return;
        axis.CommandPos = Math.Round(axis.CommandPos + deltaMm, 3);
        axis.ActualPos = axis.CommandPos;
    }

    public void HomeAxis(string axisName)
    {
        if (Runtime.IsSequenceRunning) return;
        var axis = Runtime.Axes.FirstOrDefault(a => a.AxisName == axisName);
        if (axis is null) return;
        axis.CommandPos = 0;
        axis.ActualPos = 0;
        axis.IsHomed = true;
    }

    public string RunBusopCheck(BusopCheckItem item, double simulatedValue)
    {
        item.Measured = Math.Round(simulatedValue, 4);
        item.Judge = (simulatedValue >= item.Lsl && simulatedValue <= item.Usl) ? "PASS" : "NG";
        return item.Judge;
    }

    public void RunCalibration9Point()
    {
        CalibOffsetX = 0.002;
        CalibOffsetY = -0.001;
        CalibThetaDeg = 0.0005;
        CalibResult = $"[{DateTime.Now:HH:mm:ss}] 9-Point Vision Calibration PASS (RMS Residual = 0.0018 mm)";
    }
}

public sealed class RecipeViewModel : ObservableObject
{
    private string _selectedTab = "Stage_Main";
    private string _selectedMotionToAdd = "WMX3_MOTION_STAGE";
    private MetalModelItem? _selectedModel;

    public MachineRuntime Runtime { get; }
    public TeachingRecipeData TeachingData { get; private set; }
    public MainRecipeData MainRecipe { get; private set; }
    public ObservableCollection<string> TabNames { get; } = [];
    public ObservableCollection<TeachingMotionCard> ActiveTabCards { get; } = [];
    public ObservableCollection<MetalModelItem> Models { get; } =
    [
        new() { ModelId = "Metal-V54-BSI", ProductCode = "HSG-V54-PRO", LengthMm = 159.9, WidthMm = 76.7, ScrewCount = 4, EnableMps = true },
        new() { ModelId = "Metal-V54-STD", ProductCode = "HSG-V54-STD", LengthMm = 147.6, WidthMm = 71.6, ScrewCount = 4, EnableMps = true },
        new() { ModelId = "Metal-P18-ULTRA", ProductCode = "HSG-P18-ULT", LengthMm = 162.8, WidthMm = 77.6, ScrewCount = 6, EnableMps = false }
    ];

    public RecipeViewModel(MachineRuntime runtime)
    {
        Runtime = runtime;
        TeachingData = runtime.Storage.LoadTeachingRecipe();
        MainRecipe = runtime.Storage.LoadMainRecipe();
        _selectedModel = Models[0];
        SyncTabs();
    }

    public string SelectedTab
    {
        get => _selectedTab;
        set
        {
            if (SetProperty(ref _selectedTab, value))
                RefreshActiveCards();
        }
    }

    public string SelectedMotionToAdd { get => _selectedMotionToAdd; set => SetProperty(ref _selectedMotionToAdd, value); }
    public MetalModelItem? SelectedModel { get => _selectedModel; set => SetProperty(ref _selectedModel, value); }

    public void AddTab(string tabName)
    {
        if (string.IsNullOrWhiteSpace(tabName) || TeachingData.TabNames.Contains(tabName)) return;
        TeachingData.TabNames.Add(tabName);
        TabNames.Add(tabName);
        SelectedTab = tabName;
    }

    public void DeleteSelectedTab()
    {
        if (TeachingData.TabNames.Count <= 1) return;
        TeachingData.Cards.RemoveAll(c => c.TabName == SelectedTab);
        TeachingData.TabNames.Remove(SelectedTab);
        SyncTabs();
    }

    public void AddMotionCardToCurrentTab()
    {
        if (ActiveTabCards.Any(c => c.MotionId == SelectedMotionToAdd)) return;
        string[] posNames = ["Loading", "Processing", "Unloading", "Align_Ready", "Align", "Align_Unload"];
        var card = new TeachingMotionCard
        {
            TabName = SelectedTab,
            MotionId = SelectedMotionToAdd,
            Points = posNames.Select((n, i) => new TeachingPoint { Name = n, TargetX = i * 25.0, TargetY = 10.0, TargetZ = 0.0 }).ToList()
        };
        TeachingData.Cards.Add(card);
        RefreshActiveCards();
    }

    public void CloneCardToTab(TeachingMotionCard source, string targetTab)
    {
        AddTab(targetTab);
        TeachingData.Cards.RemoveAll(c => c.TabName == targetTab && c.MotionId == source.MotionId);
        TeachingData.Cards.Add(new TeachingMotionCard
        {
            TabName = targetTab,
            MotionId = source.MotionId,
            Points = source.Points.Select(p => p.Clone()).ToList()
        });
        RefreshActiveCards();
    }

    public void Reload()
    {
        TeachingData = Runtime.Storage.LoadTeachingRecipe();
        MainRecipe = Runtime.Storage.LoadMainRecipe();
        SyncTabs();
    }

    public void SaveAll()
    {
        Runtime.Storage.SaveTeachingRecipe(TeachingData);
        Runtime.Storage.SaveMainRecipe(MainRecipe);
    }

    private void SyncTabs()
    {
        TabNames.Clear();
        foreach (var t in TeachingData.TabNames) TabNames.Add(t);
        SelectedTab = TabNames.FirstOrDefault() ?? "Stage_Main";
        RefreshActiveCards();
    }

    private void RefreshActiveCards()
    {
        ActiveTabCards.Clear();
        foreach (var c in TeachingData.Cards.Where(c => c.TabName == _selectedTab))
            ActiveTabCards.Add(c);
    }
}

public sealed class CpkCardViewItem(PdcaMeasurementItem item, SpcResult spc)
{
    public PdcaMeasurementItem Item { get; } = item;
    public SpcResult Spc { get; } = spc;
    public string SummaryLine => $"N={Spc.N} | Mean={Spc.Mean:F4} | Std={Spc.StdDev:F4} | Cp={Spc.Cp:F2} | Cpk={Spc.Cpk:F2}";
}

public sealed class MonitoringViewModel : ObservableObject
{
    private DateTime _startDay = DateTime.Today;
    private DateTime _endDay = DateTime.Today.AddDays(1);
    private int _fromHour = 0;
    private int _toHour = 23;
    private DateTime _selectedMetricsDate = DateTime.Today;

    public MachineRuntime Runtime { get; }
    public ObservableCollection<CpkCardViewItem> ShownCpkCards { get; } = [];
    public int[] HourItems { get; } = Enumerable.Range(0, 24).ToArray();

    public DateTime StartDay { get => _startDay; set => SetProperty(ref _startDay, value); }
    public DateTime EndDay { get => _endDay; set => SetProperty(ref _endDay, value); }
    public int FromHour { get => _fromHour; set => SetProperty(ref _fromHour, value); }
    public int ToHour { get => _toHour; set => SetProperty(ref _toHour, value); }
    public DateTime SelectedMetricsDate { get => _selectedMetricsDate; set => SetProperty(ref _selectedMetricsDate, value); }

    public double AvailabilityPct => 96.4;
    public double PerformancePct => 94.8;
    public double QualityPct => Math.Round(100.0 * Runtime.PassCount / Math.Max(1, Runtime.PassCount + Runtime.NgCount), 2);
    public double OeePct => Math.Round(AvailabilityPct * PerformancePct * QualityPct / 10000.0, 2);

    public MonitoringViewModel(MachineRuntime runtime)
    {
        Runtime = runtime;
        ApplySelectedItems(DateTime.Now.AddDays(-1), DateTime.Now.AddDays(1));
    }

    public List<(PdcaMeasurementItem Item, SpcResult Spc)> ApplySelectedItems(DateTime from, DateTime to)
    {
        ShownCpkCards.Clear();
        var results = new List<(PdcaMeasurementItem, SpcResult)>();

        foreach (var src in Runtime.PdcaItems.Where(x => x.IsShown))
        {
            var filtered = new PdcaMeasurementItem
            {
                Name = src.Name,
                Unit = src.Unit,
                LSL = src.LSL,
                USL = src.USL,
                DataPoints = src.DataPoints.Where(p => p.TimeStamp >= from && p.TimeStamp <= to).ToList()
            };
            var spc = SpcCalculator.Calculate(filtered, useSampleStd: true);
            ShownCpkCards.Add(new CpkCardViewItem(filtered, spc));
            results.Add((filtered, spc));
        }
        return results;
    }

    public string ExportCpkCsv(DateTime from, DateTime to)
    {
        var list = ApplySelectedItems(from, to);
        return Runtime.Storage.ExportCsv(
            $"FATP_CPK_{DateTime.Now:yyyyMMdd_HHmmss}.csv",
            ["Item", "Unit", "LSL", "USL", "N", "Mean", "StdDev", "Cp", "Cpk", "UPH"],
            list.Select(x => new[]
            {
                x.Item.Name, x.Item.Unit,
                x.Item.LSL.ToString("F4"), x.Item.USL.ToString("F4"),
                x.Spc.N.ToString(), x.Spc.Mean.ToString("F4"),
                x.Spc.StdDev.ToString("F4"), x.Spc.Cp.ToString("F3"),
                x.Spc.Cpk.ToString("F3"), x.Spc.UPH.ToString("F1")
            }));
    }

    public string ExportPerformanceCsv()
        => Runtime.Storage.ExportCsv(
            $"PerformanceMetrics_{DateTime.Now:yyyyMMdd_HHmmss}.csv",
            ["Date", "Station", "AvailabilityPct", "PerformancePct", "QualityPct", "OeePct", "UPD", "UPH"],
            [
                [SelectedMetricsDate.ToString("yyyy-MM-dd"), "HSGProcess", AvailabilityPct.ToString("F2"), PerformancePct.ToString("F2"), QualityPct.ToString("F2"), OeePct.ToString("F2"), Runtime.Upd.ToString(), Runtime.Uph.ToString()],
                [SelectedMetricsDate.ToString("yyyy-MM-dd"), "MPSProcess", "97.10", "95.20", "99.10", "91.61", Runtime.Upd.ToString(), Runtime.Uph.ToString()]
            ]);
}

public sealed class AlarmViewModel : ObservableObject
{
    private string _searchKeyword = "";
    public MachineRuntime Runtime { get; }
    public ObservableCollection<AlarmCatalogEntry> FilteredCatalog { get; } = [];
    public ObservableCollection<string> ValidationMessages { get; } = [];

    public string SearchKeyword
    {
        get => _searchKeyword;
        set
        {
            if (SetProperty(ref _searchKeyword, value))
                SearchCatalog(value);
        }
    }

    public AlarmViewModel(MachineRuntime runtime)
    {
        Runtime = runtime;
        SearchCatalog("");
        ValidateAlarmStandard();
    }

    public List<AlarmCatalogEntry> SearchCatalog(string keyword)
    {
        FilteredCatalog.Clear();
        var matches = Runtime.AlarmCatalog.Where(a =>
            string.IsNullOrWhiteSpace(keyword) ||
            a.Code.ToString().Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
            a.DescEn.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
            a.DescKo.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
            a.DescZh.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
            a.Unit.Contains(keyword, StringComparison.OrdinalIgnoreCase)).ToList();

        foreach (var m in matches) FilteredCatalog.Add(m);
        return matches;
    }

    public List<string> ValidateAlarmStandard()
    {
        ValidationMessages.Clear();
        var issues = new List<string>();
        foreach (var a in Runtime.AlarmCatalog)
        {
            if (string.IsNullOrWhiteSpace(a.DescEn) || string.IsNullOrWhiteSpace(a.DescKo) || string.IsNullOrWhiteSpace(a.DescZh))
                issues.Add($"Code {a.Code}: Missing multilingual description (EN/KO/ZH required by HIVE 3.1).");
            if (string.IsNullOrWhiteSpace(a.SolutionEn))
                issues.Add($"Code {a.Code}: Missing operator recovery solution.");
        }

        if (issues.Count == 0)
            ValidationMessages.Add($"[PASS] All {Runtime.AlarmCatalog.Count} alarm codes comply with HIVE Alarm Standard Format v1.1 (EN/KO/ZH descriptions & recovery solutions verified).");
        else
            foreach (var i in issues) ValidationMessages.Add(i);

        return issues;
    }
}

public sealed class SettingViewModel : ObservableObject
{
    private string _sparePartsVersion = "1.0.0.16";
    private string _lastBackupStatus = "No backup created in this session.";

    public MachineRuntime Runtime { get; }
    public string SparePartsVersion { get => _sparePartsVersion; set => SetProperty(ref _sparePartsVersion, value); }
    public string LastBackupStatus { get => _lastBackupStatus; set => SetProperty(ref _lastBackupStatus, value); }

    public SettingViewModel(MachineRuntime runtime) => Runtime = runtime;

    public string GenerateSparePartNamesCode()
    {
        var sb = new StringBuilder();
        sb.AppendLine("public static class SparePartNames");
        sb.AppendLine("{");
        foreach (var sp in Runtime.SpareParts)
        {
            var enPart = sp.Name.Split(';')[0].Trim().Replace(' ', '_').Replace('-', '_');
            if (string.IsNullOrEmpty(enPart)) enPart = "Part";
            sb.AppendLine($"    public const string {enPart} = \"{sp.Name}\";");
        }
        sb.AppendLine("}");
        return sb.ToString();
    }

    public bool TryAddLogConfig(string name, string level)
    {
        // v2.6.5.1 rule: up to 20 log configs supported
        if (Runtime.LogConfigs.Count >= 20 || string.IsNullOrWhiteSpace(name))
            return false;
        Runtime.LogConfigs.Add(new LogConfigItem
        {
            SlotNo = Runtime.LogConfigs.Count + 1,
            LoggerName = name,
            Level = level,
            RetentionDays = 30,
            Enabled = true
        });
        return true;
    }

    public void SaveAllSettings()
    {
        Runtime.Storage.SaveEquipment(Runtime.Equipment);
        Runtime.Storage.SaveHiveConfig(Runtime.HiveCfg);
        Runtime.Storage.SaveSpareParts(Runtime.SpareParts);
        Runtime.Storage.AppendHivePayload(Runtime.HiveCfg, "machine_parameter_changed",
            $"{{\"spare_parts_version\":\"{SparePartsVersion}\",\"site\":\"{Runtime.Equipment.Site}\"}}");
    }

    public string RunBackupNow()
    {
        var zip = Runtime.Storage.CreateBackupZip("ManualSG");
        LastBackupStatus = $"Saved: {Path.GetFileName(zip)} ({DateTime.Now:HH:mm:ss})";
        return zip;
    }
}
