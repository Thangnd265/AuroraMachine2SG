using AuroraMachine2SG.Core.ViewModels;

namespace AuroraMachine2SG.Core.Models;

public enum MachineSite { FXCN, TEMD, THER, FXBL, FXGL }
public enum MachineMode { PDCA_ONLINE, PDCA_OFFLINE, CPK, GRR, DRYRUN }
public enum HiveState { Idle, Running, Engineering, PlannedDT, UnplannedDT, Degraded }
public enum PermissionLevel { NONE = 0, Operator = 1, LineLeader = 3, Engineer = 5, Admin = 8 }
public enum WorkingPos { Loading, Align, Process, Inspection, Unloading, MPSLoading, MPSProcess }

public sealed class EquipmentConfig
{
    public string EquipmentName { get; set; } = "RSMS-001";
    public string Location { get; set; } = "FATP-B2";
    public string Site { get; set; } = MachineSite.FXCN.ToString();
    public List<string> SiteList { get; set; } = Enum.GetNames<MachineSite>().ToList();
    public string LineNo { get; set; } = "1";
    public string MachineNo { get; set; } = "1";
    public bool SimulationMode { get; set; } = true;
    public int SequenceMode { get; set; } = 0; // 0: Normal, 1: Dryrun
    public string Language { get; set; } = "en-US";
    public bool UseScreenLock { get; set; } = false;
    public bool UseVirtualKeyboard { get; set; } = false;
    public bool AutomaticallyConnectDevices { get; set; } = true;
    public string AlarmDescriptionFileName { get; set; } = "AlarmDescriptions.json";
    public int HistoryViewLimit { get; set; } = 100;
    public int ShiftResetHour { get; set; } = 8;
}

public sealed class HiveConfig
{
    public string MachineSn { get; set; } = "LGPRI-RSMS-001-2026";
    public string ServerIp { get; set; } = "10.0.0.2";
    public int ServerPort { get; set; } = 5008;
    public string EmulatorIp { get; set; } = "127.0.0.1";
    public int EmulatorPort { get; set; } = 8080;
    public bool UseDualEmulatorLogging { get; set; } = true;
    public string SopFilePath { get; set; } = @"D:\HIVE\SOP\RepairSOP_v1.0.0.0.pptx";
    public int WeeklyMaintRemainDays { get; set; } = 5;
    public int MonthlyMaintRemainDays { get; set; } = 22;
    public List<string> MaintenanceChecklistItems { get; set; } =
    [
        "1. Clean Vision Lens & Coaxial Illumination;清洁视觉镜头与同轴光源",
        "2. Inspect WMX3 Gantry Guide Rail Lubrication;检查WMX3龙门导轨润滑",
        "3. Verify ScrewDriver Torque Calibration Jig;校验电批扭力治具",
        "4. Check Vacuum Suction Nozzle Wear & Pressure;检查吸嘴磨损及负压",
        "5. Verify Safety Door Interlock & EMO Circuit;确认安全门联锁与急停回路"
    ];
}

public sealed class DeviceEntry : ObservableObject
{
    private bool _isActivated;
    private bool _isSimul = true;
    private string _statusMessage = "Ready (Simulation)";

    public string DeviceId { get; set; } = "";
    public string Category { get; set; } = "Hardware";
    public string EndpointOrPort { get; set; } = "SIMUL";
    public bool ShowInTopBar { get; set; }

    public bool IsActivated
    {
        get => _isActivated;
        set => SetProperty(ref _isActivated, value);
    }

    public bool IsSimul
    {
        get => _isSimul;
        set => SetProperty(ref _isSimul, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }
}

public sealed class StationSlot : ObservableObject
{
    private bool _hasProduct;
    private int _no;
    private string _serialNo = "";
    private string _wipCode = "";
    private string _stepText = "IDLE";
    private bool _isPass = true;

    public string Position { get; set; } = "";
    public int StationGroup { get; set; } // 0 = HSG, 1 = MPS

    public bool HasProduct { get => _hasProduct; set => SetProperty(ref _hasProduct, value); }
    public int No { get => _no; set => SetProperty(ref _no, value); }
    public string SerialNo { get => _serialNo; set => SetProperty(ref _serialNo, value); }
    public string WipCode { get => _wipCode; set => SetProperty(ref _wipCode, value); }
    public string StepText { get => _stepText; set => SetProperty(ref _stepText, value); }
    public bool IsPass { get => _isPass; set => SetProperty(ref _isPass, value); }
}

public sealed class ModuleHistoryItem
{
    public string Timestamp { get; set; } = "";
    public string SerialNo { get; set; } = "";
    public string Station { get; set; } = "HSG";
    public double CycleSec { get; set; }
    public double PeakTorqueNm { get; set; }
    public string Judge { get; set; } = "PASS";
}

public sealed class TrayCell : ObservableObject
{
    private bool _used;
    public int Index { get; set; }
    public string Label { get; set; } = "";
    public bool Used { get => _used; set => SetProperty(ref _used, value); }
}

public sealed class AxisStatus : ObservableObject
{
    private double _actualPos;
    private double _commandPos;
    private double _velocity = 120.0;
    private double _loadPercent = 18.5;
    private bool _servoOn = true;
    private bool _isHomed = true;

    public string AxisName { get; set; } = "";
    public string MotionGroup { get; set; } = "WMX3_MOTION_STAGE";
    public double ActualPos { get => _actualPos; set => SetProperty(ref _actualPos, value); }
    public double CommandPos { get => _commandPos; set => SetProperty(ref _commandPos, value); }
    public double Velocity { get => _velocity; set => SetProperty(ref _velocity, value); }
    public double LoadPercent { get => _loadPercent; set => SetProperty(ref _loadPercent, value); }
    public bool ServoOn { get => _servoOn; set => SetProperty(ref _servoOn, value); }
    public bool IsHomed { get => _isHomed; set => SetProperty(ref _isHomed, value); }
}

public sealed class TeachingPoint : ObservableObject
{
    private double _targetX;
    private double _targetY;
    private double _targetZ;
    private double _velocity = 150.0;
    private double _accel = 1000.0;
    private double _decel = 1000.0;

    public string Name { get; set; } = "";
    public double TargetX { get => _targetX; set => SetProperty(ref _targetX, value); }
    public double TargetY { get => _targetY; set => SetProperty(ref _targetY, value); }
    public double TargetZ { get => _targetZ; set => SetProperty(ref _targetZ, value); }
    public double Velocity { get => _velocity; set => SetProperty(ref _velocity, value); }
    public double Accel { get => _accel; set => SetProperty(ref _accel, value); }
    public double Decel { get => _decel; set => SetProperty(ref _decel, value); }

    public TeachingPoint Clone() => new()
    {
        Name = Name,
        TargetX = TargetX,
        TargetY = TargetY,
        TargetZ = TargetZ,
        Velocity = Velocity,
        Accel = Accel,
        Decel = Decel
    };
}

public sealed class TeachingMotionCard
{
    public string TabName { get; set; } = "Stage_Main";
    public string MotionId { get; set; } = "WMX3_MOTION_STAGE";
    public List<TeachingPoint> Points { get; set; } = [];
}

public sealed class TeachingRecipeData
{
    public List<string> TabNames { get; set; } = ["Stage_Main", "Head_Assy", "MPS_Sub"];
    public List<string> MotionIds { get; set; } = ["WMX3_MOTION_STAGE", "WMX3_MOTION_HEAD", "WMX3_MOTION_TEACHINGSAMPLE"];
    public List<TeachingMotionCard> Cards { get; set; } = [];
}

public sealed class MainRecipeData
{
    public string ActiveModelName { get; set; } = "Metal-V54-BSI";
    public double TargetScrewTorqueNm { get; set; } = 2.85;
    public double ScrewTorqueNgLimitNm { get; set; } = 3.80;
    public double VisionExposureMs { get; set; } = 12.5;
    public double VisionGainDb { get; set; } = 3.0;
    public double AlignToleranceMm { get; set; } = 0.035;
    public int MaxRetryCount { get; set; } = 2;
}

public sealed class MetalModelItem
{
    public string ModelId { get; set; } = "Metal-V54-BSI";
    public string ProductCode { get; set; } = "HSG-V54-PRO";
    public double LengthMm { get; set; } = 159.9;
    public double WidthMm { get; set; } = 76.7;
    public int ScrewCount { get; set; } = 4;
    public bool EnableMps { get; set; } = true;
}

public sealed class PdcaDataPoint
{
    public DateTime TimeStamp { get; set; }
    public double Value { get; set; }
    public string SerialNo { get; set; } = "";
}

public sealed class PdcaMeasurementItem : ObservableObject
{
    private bool _isShown = true;
    public string Name { get; set; } = "";
    public string Unit { get; set; } = "mm";
    public double LSL { get; set; }
    public double USL { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public List<PdcaDataPoint> DataPoints { get; set; } = [];

    public bool IsShown
    {
        get => _isShown;
        set => SetProperty(ref _isShown, value);
    }
}

public sealed class AlarmCatalogEntry
{
    public int Code { get; set; }
    public string Category { get; set; } = "Heavy";
    public string Unit { get; set; } = "SGMachine";
    public string DescEn { get; set; } = "";
    public string DescKo { get; set; } = "";
    public string DescZh { get; set; } = "";
    public string SolutionEn { get; set; } = "";
    public string SolutionKo { get; set; } = "";
    public string SolutionZh { get; set; } = "";
}

public sealed class AlarmHistoryRecord
{
    public DateTime Time { get; set; }
    public int Code { get; set; }
    public string Category { get; set; } = "Heavy";
    public string Station { get; set; } = "HSGProcess";
    public string Description { get; set; } = "";
    public string ClearedByBadge { get; set; } = "";
    public double DowntimeMinutes { get; set; }
}

public sealed class TossingOrNgRecord
{
    public DateTime Time { get; set; }
    public string Kind { get; set; } = "NG"; // "Tossing" or "NG"
    public string Station { get; set; } = "HSG";
    public string SerialNo { get; set; } = "";
    public string PartSn { get; set; } = "";
    public string Reason { get; set; } = "";
}

public sealed class SparePartItem : ObservableObject
{
    private int _currentCount;
    private int _warningLimit = 8000;
    private int _maxLimit = 10000;
    private bool _isUseAlarm = true;
    private int _alarmCode = 4101;
    private string _name = "camera;相机";

    public string Unit { get; set; } = "Vision";
    public string Name { get => _name; set => SetProperty(ref _name, value); }
    public string PartNumber { get; set; } = "SP-PRI-001";
    public bool IsUseAlarm { get => _isUseAlarm; set => SetProperty(ref _isUseAlarm, value); }
    public int AlarmCode { get => _alarmCode; set => SetProperty(ref _alarmCode, value); }
    public int CurrentCount { get => _currentCount; set { if (SetProperty(ref _currentCount, value)) OnPropertyChanged(nameof(IsAlarmed)); } }
    public int WarningLimit { get => _warningLimit; set => SetProperty(ref _warningLimit, value); }
    public int MaxLimit { get => _maxLimit; set { if (SetProperty(ref _maxLimit, value)) OnPropertyChanged(nameof(IsAlarmed)); } }
    public bool IsAlarmed => IsUseAlarm && CurrentCount >= MaxLimit;
}

public sealed class LogConfigItem : ObservableObject
{
    private bool _enabled = true;
    public int SlotNo { get; set; }
    public string LoggerName { get; set; } = "";
    public string Level { get; set; } = "INFO";
    public int RetentionDays { get; set; } = 30;
    public bool Enabled { get => _enabled; set => SetProperty(ref _enabled, value); }
}

public sealed class BusopCheckItem : ObservableObject
{
    private double _measured;
    private string _judge = "READY";
    public string SopGroup { get; set; } = ""; // Force, LoadCell, VisionStatic, VisionDynamic
    public string SequenceId { get; set; } = "";
    public double Lsl { get; set; }
    public double Usl { get; set; }
    public string Unit { get; set; } = "N";
    public double Measured { get => _measured; set => SetProperty(ref _measured, value); }
    public string Judge { get => _judge; set => SetProperty(ref _judge, value); }
}

public sealed class IoBitItem : ObservableObject
{
    private bool _state;
    public int Address { get; set; }
    public string Name { get; set; } = "";
    public bool IsOutput { get; set; }
    public bool State { get => _state; set => SetProperty(ref _state, value); }
}
