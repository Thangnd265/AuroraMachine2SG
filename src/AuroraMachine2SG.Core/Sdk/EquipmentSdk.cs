using System.Reflection;
using AuroraMachine2SG.Core.Models;

namespace AuroraMachine2SG.Core.Sdk;

public enum MachineTopology { Single, MultiSequence, MultiFixture }

public interface IEquipmentSetup
{
    string ProfileId { get; }
    string DisplayTitle { get; }
    string EquipmentName { get; }
    MachineTopology Topology { get; }
    IReadOnlyList<string> StationNames { get; }
    IReadOnlyList<DeviceEntry> CreateDevices();
    IReadOnlyList<SequenceRunner> CreateSequences(SequenceContext context);
}

/// <summary>
/// Profile 1: Exact AuroraMachine2SG (RSMS-001 MultiSequence HSG + MPS + BUSOP)
/// </summary>
public sealed class SGMachineMultiSequenceSetup : IEquipmentSetup
{
    public string ProfileId => "SGMachine_MultiSequence";
    public string DisplayTitle => "RSMS-001 | SG MultiSequence (HSG + MPS)";
    public string EquipmentName => "RSMS-001";
    public MachineTopology Topology => MachineTopology.MultiSequence;
    public IReadOnlyList<string> StationNames => ["HSGProcess", "MPSProcess"];

    public IReadOnlyList<DeviceEntry> CreateDevices() => DefaultSgDevices();

    public IReadOnlyList<SequenceRunner> CreateSequences(SequenceContext ctx) =>
    [
        new SequenceBuilder("Loading")
            .AddAction("LOADING_INIT")
            .AddAction("LOADING_MOVE_POS")
            .AddDecision("LOADING_LOAD", (c, _) => c.StateBag.ContainsKey("ManualLoadMode") ? "LOADING_MANUALLOAD" : "LOADING_CHECK_BCR")
            .AddEventAcceptor("LOADING_MANUALLOAD", "EVENT_MANUAL_LOADING")
            .AddAction("LOADING_CHECK_BCR")
            .AddAction("LOADING_TRANSFER")
            .AddDecision("CHECK_CYCLESTOP", (c, _) => c.IsCycleStopRequested ? "LOADING_COMPLETE" : "LOADING_INIT")
            .AddAction("LOADING_COMPLETE")
            .Flow("Init", "LOADING_INIT", "LOADING_MOVE_POS", "LOADING_LOAD", "LOADING_CHECK_BCR", "LOADING_TRANSFER", "CHECK_CYCLESTOP", "LOADING_COMPLETE", "Final")
            .Flow("LOADING_LOAD", "LOADING_MANUALLOAD", "LOADING_CHECK_BCR")
            .Flow("CHECK_CYCLESTOP", "LOADING_INIT")
            .Build(ctx),

        new SequenceBuilder("Align")
            .AddDecision("CHECK_ALIGN_POS", (_, _) => "ALIGN_MOVE_CAM")
            .AddAction("ALIGN_MOVE_CAM")
            .AddAction("COGNEX_T31_GRAB")
            .AddAction("CALC_UVW_OFFSET")
            .AddDecision("CHECK_CYCLESTOP", (c, _) => c.IsCycleStopRequested ? "ALIGN_COMPLETE" : "CHECK_ALIGN_POS")
            .AddAction("ALIGN_COMPLETE")
            .Flow("Init", "CHECK_ALIGN_POS", "ALIGN_MOVE_CAM", "COGNEX_T31_GRAB", "CALC_UVW_OFFSET", "CHECK_CYCLESTOP", "ALIGN_COMPLETE", "Final")
            .Flow("CHECK_CYCLESTOP", "CHECK_ALIGN_POS")
            .Build(ctx),

        new SequenceBuilder("Process")
            .AddDecision("SEQ_CHECK_WORK_POS", (_, _) => "SEQ_NODE_INIT")
            .AddAction("SEQ_NODE_INIT")
            .AddAction("GANTRY_SCREW_FASTEN")
            .AddAction("VERIFY_TORQUE_CURVE")
            .AddAction("SEQ_PROCESS_UNLOAD")
            .AddDecision("SEQ_CHECK_CYCLESTOP", (c, _) => c.IsCycleStopRequested ? "SEQ_NODE_COMPLETE" : "SEQ_CHECK_WORK_POS")
            .AddAction("SEQ_NODE_COMPLETE")
            .Flow("Init", "SEQ_CHECK_WORK_POS", "SEQ_NODE_INIT", "GANTRY_SCREW_FASTEN", "VERIFY_TORQUE_CURVE", "SEQ_PROCESS_UNLOAD", "SEQ_CHECK_CYCLESTOP", "SEQ_NODE_COMPLETE", "Final")
            .Flow("SEQ_CHECK_CYCLESTOP", "SEQ_CHECK_WORK_POS")
            .Build(ctx),

        new SequenceBuilder("MPSLoading")
            .AddAction("MPS_PICK_TRAY")
            .AddAction("MPS_RFID_VERIFY")
            .AddDecision("MPS_CYCLE_CHECK", (c, _) => c.IsCycleStopRequested ? "Final" : "MPS_PICK_TRAY")
            .Flow("Init", "MPS_PICK_TRAY", "MPS_RFID_VERIFY", "MPS_CYCLE_CHECK", "Final")
            .Flow("MPS_CYCLE_CHECK", "MPS_PICK_TRAY")
            .Build(ctx),

        new SequenceBuilder("MPSProcess")
            .AddAction("MPS_PRE_ALIGN")
            .AddAction("MPS_PLACE_TO_HSG")
            .AddDecision("MPS_PROC_LOOP", (c, _) => c.IsCycleStopRequested ? "Final" : "MPS_PRE_ALIGN")
            .Flow("Init", "MPS_PRE_ALIGN", "MPS_PLACE_TO_HSG", "MPS_PROC_LOOP", "Final")
            .Flow("MPS_PROC_LOOP", "MPS_PRE_ALIGN")
            .Build(ctx),

        new SequenceBuilder("Inspection")
            .AddAction("KEYENCE_DYNAMIC_SCAN")
            .AddAction("COLLECT_PDCA_CPK")
            .AddDecision("JUDGE_SPEC", (_, _) => "PUSH_BALI_RESULT")
            .AddAction("PUSH_BALI_RESULT")
            .AddDecision("INSP_LOOP", (c, _) => c.IsCycleStopRequested ? "Final" : "KEYENCE_DYNAMIC_SCAN")
            .Flow("Init", "KEYENCE_DYNAMIC_SCAN", "COLLECT_PDCA_CPK", "JUDGE_SPEC", "PUSH_BALI_RESULT", "INSP_LOOP", "Final")
            .Flow("INSP_LOOP", "KEYENCE_DYNAMIC_SCAN")
            .Build(ctx),

        new SequenceBuilder("Unloading")
            .AddAction("SORT_TRAY_OR_NG")
            .AddAction("HERMES_DOWNSTREAM_SEND")
            .AddDecision("UNLOAD_LOOP", (c, _) => c.IsCycleStopRequested ? "Final" : "SORT_TRAY_OR_NG")
            .Flow("Init", "SORT_TRAY_OR_NG", "HERMES_DOWNSTREAM_SEND", "UNLOAD_LOOP", "Final")
            .Flow("UNLOAD_LOOP", "SORT_TRAY_OR_NG")
            .Build(ctx)
    ];

    internal static List<DeviceEntry> DefaultSgDevices() =>
    [
        new() { DeviceId = "WMX3_ENGINE", Category = "Motion", EndpointOrPort = "RTX64-EtherCAT", ShowInTopBar = true, IsActivated = true },
        new() { DeviceId = "AXTLibrary", Category = "Motion", EndpointOrPort = "PCIe-AXT", ShowInTopBar = true, IsActivated = true },
        new() { DeviceId = "BCR", Category = "AutoID", EndpointOrPort = "192.168.1.50:23", ShowInTopBar = true, IsActivated = true },
        new() { DeviceId = "HermesUpstream", Category = "Hermes", EndpointOrPort = "TCP:50100", ShowInTopBar = true, IsActivated = true },
        new() { DeviceId = "HermesDownstream", Category = "Hermes", EndpointOrPort = "TCP:50101", IsActivated = true },
        new() { DeviceId = "BALIConnDevice", Category = "MES", EndpointOrPort = "10.0.0.10:5500", IsActivated = true },
        new() { DeviceId = "HIVE", Category = "MES", EndpointOrPort = "10.0.0.2:5008", IsActivated = true },
        new() { DeviceId = "SGML", Category = "AI/ML", EndpointOrPort = "Local-ONNX", IsActivated = true },
        new() { DeviceId = "KeyenceVisionIF", Category = "Vision", EndpointOrPort = "192.168.1.61:8500", IsActivated = true },
        new() { DeviceId = "CognexVisionIF", Category = "Vision", EndpointOrPort = "192.168.1.62:3000", IsActivated = true },
        new() { DeviceId = "CardReader", Category = "Badge", EndpointOrPort = "USB-HID-JRK6", IsActivated = true },
        new() { DeviceId = "RFID", Category = "AutoID", EndpointOrPort = "COM4-115200", IsActivated = false },
        new() { DeviceId = "ScrewDriver", Category = "Actuator", EndpointOrPort = "COM6-57600", IsActivated = true },
        new() { DeviceId = "WMX3_IO_SAMPLE", Category = "MotionIO", EndpointOrPort = "EtherCAT-Slave01", IsActivated = true },
        new() { DeviceId = "WMX3_MOTION_HEAD", Category = "Motion", EndpointOrPort = "Axis 0..5", IsActivated = true },
        new() { DeviceId = "WMX3_MOTION_STAGE", Category = "Motion", EndpointOrPort = "Axis 6..11", IsActivated = true },
        new() { DeviceId = "DiskCleaner", Category = "Utility", EndpointOrPort = @"D:\LogCleaner", IsActivated = true },
        new() { DeviceId = "MachineDataDB", Category = "Database", EndpointOrPort = "LocalJsonStore", IsActivated = true },
        new() { DeviceId = "CPKCollector", Category = "SPC", EndpointOrPort = "PDCA-Collector", IsActivated = true },
        new() { DeviceId = "MotorLoadDB", Category = "Telemetry", EndpointOrPort = "DataGravityHub", IsActivated = true }
    ];
}

/// <summary>
/// Profile 2: MultiFixture 4-Bay Topology (Background + BAY1..BAY4 + Controller from EquipmentSetup.cs)
/// </summary>
public sealed class SGMachineMultiFixtureSetup : IEquipmentSetup
{
    public string ProfileId => "SGMachine_MultiFixture4Bay";
    public string DisplayTitle => "RSMS-4BAY | SG MultiFixture (Background + BAY1..4 + Controller)";
    public string EquipmentName => "RSMS-4BAY";
    public MachineTopology Topology => MachineTopology.MultiFixture;
    public IReadOnlyList<string> StationNames => ["Background", "BAY1", "BAY2", "BAY3", "BAY4", "Controller"];

    public IReadOnlyList<DeviceEntry> CreateDevices() => SGMachineMultiSequenceSetup.DefaultSgDevices();

    public IReadOnlyList<SequenceRunner> CreateSequences(SequenceContext ctx)
    {
        var list = new List<SequenceRunner>
        {
            new SequenceBuilder("Background_Transfer")
                .AddAction("GANTRY_PICK_INPUT")
                .AddDecision("DISPATCH_FREE_BAY", (_, _) => "PLACE_INTO_BAY")
                .AddAction("PLACE_INTO_BAY")
                .AddDecision("BG_LOOP", (c, _) => c.IsCycleStopRequested ? "Final" : "GANTRY_PICK_INPUT")
                .Flow("Init", "GANTRY_PICK_INPUT", "DISPATCH_FREE_BAY", "PLACE_INTO_BAY", "BG_LOOP", "Final")
                .Flow("BG_LOOP", "GANTRY_PICK_INPUT")
                .Build(ctx)
        };

        for (int bay = 1; bay <= 4; bay++)
        {
            int bayNo = bay;
            list.Add(new SequenceBuilder($"BAY{bayNo}")
                .AddAction($"BAY{bayNo}_CLAMP_FIXTURE")
                .AddAction($"BAY{bayNo}_FUNCTIONAL_TEST")
                .AddAction($"BAY{bayNo}_REPORT_PDCA")
                .AddDecision($"BAY{bayNo}_LOOP", (c, _) => c.IsCycleStopRequested ? "Final" : $"BAY{bayNo}_CLAMP_FIXTURE")
                .Flow("Init", $"BAY{bayNo}_CLAMP_FIXTURE", $"BAY{bayNo}_FUNCTIONAL_TEST", $"BAY{bayNo}_REPORT_PDCA", $"BAY{bayNo}_LOOP", "Final")
                .Flow($"BAY{bayNo}_LOOP", $"BAY{bayNo}_CLAMP_FIXTURE")
                .Build(ctx));
        }

        list.Add(new SequenceBuilder("Controller")
            .AddAction("SYNC_NORMAL_BAY_STATES")
            .AddAction("REPORT_HIVE_AGGREGATE")
            .AddDecision("CTRL_LOOP", (c, _) => c.IsCycleStopRequested ? "Final" : "SYNC_NORMAL_BAY_STATES")
            .Flow("Init", "SYNC_NORMAL_BAY_STATES", "REPORT_HIVE_AGGREGATE", "CTRL_LOOP", "Final")
            .Flow("CTRL_LOOP", "SYNC_NORMAL_BAY_STATES")
            .Build(ctx));

        return list;
    }
}

/// <summary>
/// Profile 3: Clean Starter Template for Custom Machine Development (modeled after AuroraEdu/SGPractice: Laser + Scale + Motor)
/// Edit or duplicate this class to develop any new custom machine on the platform!
/// </summary>
public sealed class CustomStarterMachineSetup : IEquipmentSetup
{
    public string ProfileId => "CustomMachine_StarterTemplate";
    public string DisplayTitle => "CUSTOM-DEV-001 | Custom Machine Starter (SGPractice Laser + Scale)";
    public string EquipmentName => "CUSTOM-DEV-001";
    public MachineTopology Topology => MachineTopology.Single;
    public IReadOnlyList<string> StationNames => ["MAIN_CUSTOM"];

    public IReadOnlyList<DeviceEntry> CreateDevices() =>
    [
        new() { DeviceId = "WMX3_ENGINE", Category = "Motion", EndpointOrPort = "Simul-2Axis", ShowInTopBar = true, IsActivated = true },
        new() { DeviceId = "SimpleLaserMock", Category = "Sensor", EndpointOrPort = "COM3-9600", ShowInTopBar = true, IsActivated = true },
        new() { DeviceId = "SimpleScaleMock", Category = "Sensor", EndpointOrPort = "COM5-19200", ShowInTopBar = true, IsActivated = true },
        new() { DeviceId = "BCR", Category = "AutoID", EndpointOrPort = "TCP:9004", ShowInTopBar = true, IsActivated = true },
        new() { DeviceId = "HIVE", Category = "MES", EndpointOrPort = "127.0.0.1:8080", IsActivated = true }
    ];

    public IReadOnlyList<SequenceRunner> CreateSequences(SequenceContext ctx) =>
    [
        new SequenceBuilder("MainCustomSeq")
            .AddAction("STEP1_HOME_CHECK")
            .AddAction("STEP2_SCAN_BARCODE")
            .AddAction("STEP3_LASER_HEIGHT_MEASURE")
            .AddAction("STEP4_WEIGHT_SCALE_CHECK")
            .AddDecision("STEP5_JUDGE_QUALITY", (_, _) => "STEP6_SAVE_RESULT")
            .AddAction("STEP6_SAVE_RESULT")
            .AddDecision("STEP7_CHECK_CYCLESTOP", (c, _) => c.IsCycleStopRequested ? "Final" : "STEP1_HOME_CHECK")
            .Flow("Init", "STEP1_HOME_CHECK", "STEP2_SCAN_BARCODE", "STEP3_LASER_HEIGHT_MEASURE", "STEP4_WEIGHT_SCALE_CHECK", "STEP5_JUDGE_QUALITY", "STEP6_SAVE_RESULT", "STEP7_CHECK_CYCLESTOP", "Final")
            .Flow("STEP7_CHECK_CYCLESTOP", "STEP1_HOME_CHECK")
            .Build(ctx)
    ];
}

public static class BuiltInEquipmentProfiles
{
    public static IReadOnlyList<IEquipmentSetup> GetAll() =>
    [
        new SGMachineMultiSequenceSetup(),
        new SGMachineMultiFixtureSetup(),
        new CustomStarterMachineSetup()
    ];

    public static IReadOnlyList<IEquipmentSetup> LoadFromAssemblyFile(string dllPath)
    {
        if (!File.Exists(dllPath)) return [];
        var asm = Assembly.LoadFrom(Path.GetFullPath(dllPath));
        return asm.GetExportedTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface && typeof(IEquipmentSetup).IsAssignableFrom(t) && t.GetConstructor(Type.EmptyTypes) is not null)
            .Select(t => (IEquipmentSetup)Activator.CreateInstance(t)!)
            .ToList();
    }
}
