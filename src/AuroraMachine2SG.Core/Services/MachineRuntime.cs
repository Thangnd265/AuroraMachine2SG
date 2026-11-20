using System.Collections.ObjectModel;
using AuroraMachine2SG.Core.Models;
using AuroraMachine2SG.Core.ViewModels;

namespace AuroraMachine2SG.Core.Services;

public sealed class MachineRuntime : ObservableObject
{
    private readonly Random _rng = new(20261120);
    private int _serialCounter = 1042;
    private DateTime _runStartedAt = DateTime.Now;

    private bool _isSequenceRunning;
    private bool _isSequencePaused;
    private bool _isDoorLocked = true;
    private bool _useMps = true;
    private bool _useHermes = true;
    private bool _isEmoTest;
    private bool _isMachineReady = true;
    private MachineMode _currentMode = MachineMode.PDCA_ONLINE;
    private int _upd;
    private int _uph;
    private double _cycleTime = 3.42;
    private int _passCount = 118;
    private int _ngCount = 4;
    private bool _noAlarm = true;
    private string _activeAlarmCode = "No Alarm";
    private string _activeAlarmCategory = "No Alarm";
    private string _activeAlarmTime = "00:00:00";
    private string _lastScrewId = "SCREW-G1-01";
    private string _lastWipCode = "WIP-V54-HSG-1042";
    private List<(double Angle, double Torque)> _lastScrewCurve = [];

    public StorageService Storage { get; }
    public LocalizationService Loc { get; } = new();
    public EquipmentConfig Equipment { get; }
    public HiveConfig HiveCfg { get; }
    public HiveStateMachine Hive { get; }

    public ObservableCollection<DeviceEntry> Devices { get; } = [];
    public ObservableCollection<StationSlot> HsgSlots { get; } = [];
    public ObservableCollection<StationSlot> MpsSlots { get; } = [];
    public ObservableCollection<StationSlot> StationMonitors { get; } = [];
    public ObservableCollection<TrayCell> CassetteCells { get; } = [];
    public ObservableCollection<TrayCell> TrayCells { get; } = [];
    public ObservableCollection<ModuleHistoryItem> LatestHistory { get; } = [];
    public ObservableCollection<AlarmCatalogEntry> AlarmCatalog { get; } = [];
    public ObservableCollection<AlarmHistoryRecord> AlarmHistory { get; } = [];
    public ObservableCollection<TossingOrNgRecord> TossingAndNgRecords { get; } = [];
    public ObservableCollection<SparePartItem> SpareParts { get; } = [];
    public ObservableCollection<PdcaMeasurementItem> PdcaItems { get; } = [];
    public ObservableCollection<AxisStatus> Axes { get; } = [];
    public ObservableCollection<IoBitItem> IoBits { get; } = [];
    public ObservableCollection<BusopCheckItem> BusopItems { get; } = [];
    public ObservableCollection<LogConfigItem> LogConfigs { get; } = [];

    public double[] HourlyCounts { get; } = [14, 16, 15, 12, 18, 22, 28, 31, 35, 38, 36, 34, 29, 37, 39, 41, 38, 33, 27, 24, 21, 19, 17, 15];
    public double[] WeeklyCounts { get; } = [612, 648, 635, 671, 690, 580, 495];

    public MachineRuntime(StorageService storage)
    {
        Storage = storage;
        Equipment = storage.LoadEquipment();
        HiveCfg = storage.LoadHiveConfig();
        Loc.SetLanguage(Equipment.Language);
        Hive = new HiveStateMachine(HiveCfg, storage);

        SeedCollections();
    }

    public bool IsSequenceRunning
    {
        get => _isSequenceRunning;
        private set
        {
            if (SetProperty(ref _isSequenceRunning, value))
                OnPropertyChanged(nameof(IsSequenceIdle));
        }
    }

    public bool IsSequencePaused { get => _isSequencePaused; private set => SetProperty(ref _isSequencePaused, value); }
    public bool IsSequenceIdle => !IsSequenceRunning && !IsSequencePaused;
    public bool IsDoorLocked { get => _isDoorLocked; private set => SetProperty(ref _isDoorLocked, value); }
    public bool UseMps { get => _useMps; set => SetProperty(ref _useMps, value); }
    public bool UseHermes { get => _useHermes; set => SetProperty(ref _useHermes, value); }
    public bool IsMachineReady { get => _isMachineReady; set => SetProperty(ref _isMachineReady, value); }

    public bool IsEmoTest
    {
        get => _isEmoTest;
        set
        {
            if (SetProperty(ref _isEmoTest, value) && value)
                RaiseAlarm(1001, isHeavy: true);
        }
    }

    public MachineMode CurrentMode { get => _currentMode; private set => SetProperty(ref _currentMode, value); }
    public int Upd { get => _upd; private set => SetProperty(ref _upd, value); }
    public int Uph { get => _uph; private set => SetProperty(ref _uph, value); }
    public double CycleTime { get => _cycleTime; private set => SetProperty(ref _cycleTime, value); }
    public int PassCount { get => _passCount; private set => SetProperty(ref _passCount, value); }
    public int NgCount { get => _ngCount; private set => SetProperty(ref _ngCount, value); }
    public bool NoAlarm { get => _noAlarm; private set => SetProperty(ref _noAlarm, value); }
    public string ActiveAlarmCode { get => _activeAlarmCode; private set => SetProperty(ref _activeAlarmCode, value); }
    public string ActiveAlarmCategory { get => _activeAlarmCategory; private set => SetProperty(ref _activeAlarmCategory, value); }
    public string ActiveAlarmTime { get => _activeAlarmTime; private set => SetProperty(ref _activeAlarmTime, value); }
    public string LastScrewId { get => _lastScrewId; private set => SetProperty(ref _lastScrewId, value); }
    public string LastWipCode { get => _lastWipCode; private set => SetProperty(ref _lastWipCode, value); }
    public List<(double Angle, double Torque)> LastScrewCurve { get => _lastScrewCurve; private set => SetProperty(ref _lastScrewCurve, value); }

    public void InitializeAllDevices(bool activateAll = true)
    {
        foreach (var dev in Devices)
        {
            // Match EquipmentSetup.InitDevice: RFID defaults inactive until manually enabled
            dev.IsActivated = activateAll && dev.DeviceId != "RFID";
            dev.StatusMessage = dev.IsActivated ? "Activated (Simul OK)" : "Standby";
        }
    }

    public bool StartProduction()
    {
        if (!IsDoorLocked || !NoAlarm) return false;
        IsSequencePaused = false;
        IsSequenceRunning = true;
        _runStartedAt = DateTime.Now;
        Hive.SetRunning();
        SetIoBit(100, true); // Tower Lamp Green
        SetIoBit(101, false);
        return true;
    }

    public void PauseProduction()
    {
        if (!IsSequenceRunning) return;
        IsSequenceRunning = false;
        IsSequencePaused = true;
        Hive.SetIdle();
        SetIoBit(100, false);
        SetIoBit(101, true); // Tower Lamp Yellow
    }

    public void StopProduction(string manualCause = "Manual Stop;手动停机")
    {
        IsSequenceRunning = false;
        IsSequencePaused = false;
        Hive.EnterManualDowntime(manualCause);
        SetIoBit(100, false);
        SetIoBit(102, true); // Tower Lamp Red
    }

    public bool TrySetDoorLocked(bool locked)
    {
        if (IsSequenceRunning) return false;
        IsDoorLocked = locked;
        SetIoBit(0, locked);
        return true;
    }

    public bool TryChangeMode(MachineMode mode)
    {
        if (IsSequenceRunning) return false;
        CurrentMode = mode;
        Equipment.SequenceMode = mode == MachineMode.DRYRUN ? 1 : 0;
        Storage.SaveEquipment(Equipment);
        return true;
    }

    public void StepSimulationCycle()
    {
        if (!IsSequenceRunning) return;

        // Advance HSG 5-stage pipeline: Loading -> Align -> Process -> Inspection -> Unloading
        if (HsgSlots[^1].HasProduct)
        {
            CompleteProduct(HsgSlots[^1]);
            HsgSlots[^1].HasProduct = false;
            HsgSlots[^1].StepText = "DONE";
        }

        for (int i = HsgSlots.Count - 1; i >= 1; i--)
        {
            if (HsgSlots[i - 1].HasProduct && !HsgSlots[i].HasProduct)
            {
                HsgSlots[i].HasProduct = true;
                HsgSlots[i].No = HsgSlots[i - 1].No;
                HsgSlots[i].SerialNo = HsgSlots[i - 1].SerialNo;
                HsgSlots[i].WipCode = HsgSlots[i - 1].WipCode;
                HsgSlots[i].StepText = $"RUN ({HsgSlots[i].Position})";
                HsgSlots[i - 1].HasProduct = false;
                HsgSlots[i - 1].StepText = "WAIT";
            }
        }

        if (!HsgSlots[0].HasProduct)
            SpawnProductInto(HsgSlots[0], "HSG");

        // Advance MPS 2-stage branch when UseMps is enabled
        if (UseMps && MpsSlots.Count >= 2)
        {
            if (MpsSlots[1].HasProduct)
            {
                MpsSlots[1].HasProduct = false;
                MpsSlots[1].StepText = "MERGED->HSG";
            }
            if (MpsSlots[0].HasProduct && !MpsSlots[1].HasProduct)
            {
                MpsSlots[1].HasProduct = true;
                MpsSlots[1].No = MpsSlots[0].No;
                MpsSlots[1].SerialNo = MpsSlots[0].SerialNo;
                MpsSlots[1].StepText = "MPS PROC";
                MpsSlots[0].HasProduct = false;
            }
            if (!MpsSlots[0].HasProduct)
                SpawnProductInto(MpsSlots[0], "MPS");
        }

        // Sync StationMonitors strip
        for (int i = 0; i < Math.Min(HsgSlots.Count, StationMonitors.Count); i++)
        {
            StationMonitors[i].HasProduct = HsgSlots[i].HasProduct;
            StationMonitors[i].No = HsgSlots[i].No;
            StationMonitors[i].SerialNo = HsgSlots[i].SerialNo;
        }

        // Update servo axis positions & load factors + screw curve + spare-part wear
        foreach (var ax in Axes)
        {
            double delta = Math.Round((_rng.NextDouble() - 0.45) * 8.0, 2);
            ax.CommandPos = Math.Round(Math.Clamp(ax.CommandPos + delta, -50, 420), 2);
            ax.ActualPos = ax.CommandPos;
            ax.LoadPercent = Math.Round(15.0 + _rng.NextDouble() * 42.0, 1);
        }

        GenerateScrewWaveform();
        IncrementSparePartsAndCheckAlarms();
    }

    public void ManualLoadProduct()
    {
        if (HsgSlots.Count > 0 && !HsgSlots[0].HasProduct)
            SpawnProductInto(HsgSlots[0], "MANUAL");
    }

    public void ManualUnloadProduct()
    {
        var occupied = HsgSlots.LastOrDefault(s => s.HasProduct);
        if (occupied is not null)
        {
            CompleteProduct(occupied);
            occupied.HasProduct = false;
            occupied.StepText = "MANUAL OUT";
        }
    }

    public void RemoveProductFromSlot(StationSlot? slot)
    {
        if (slot is null || !slot.HasProduct) return;
        TossingAndNgRecords.Insert(0, new TossingOrNgRecord
        {
            Time = DateTime.Now,
            Kind = "Tossing",
            Station = slot.StationGroup == 0 ? "HSG" : "MPS",
            SerialNo = slot.SerialNo,
            PartSn = $"PART-{slot.No:0000}",
            Reason = $"Operator Remove Product at {slot.Position}"
        });
        slot.HasProduct = false;
        slot.StepText = "REMOVED";
    }

    public void RaiseAlarm(int code, bool isHeavy = true)
    {
        var entry = AlarmCatalog.FirstOrDefault(a => a.Code == code)
            ?? new AlarmCatalogEntry { Code = code, Category = isHeavy ? "Heavy" : "Light", DescEn = $"Equipment Alarm {code}" };

        NoAlarm = false;
        ActiveAlarmCode = code.ToString();
        ActiveAlarmCategory = entry.Category;
        ActiveAlarmTime = DateTime.Now.ToString("HH:mm:ss");

        if (isHeavy || entry.Category is "Heavy" or "Maintenance")
        {
            IsSequenceRunning = false;
            IsSequencePaused = false;
            Hive.TriggerAutoAlarmDowntime(code, entry.DescEn);
        }

        AlarmHistory.Insert(0, new AlarmHistoryRecord
        {
            Time = DateTime.Now,
            Code = code,
            Category = entry.Category,
            Station = "HSGProcess",
            Description = entry.DescEn,
            ClearedByBadge = "ACTIVE"
        });
    }

    public double ClearActiveAlarmAndResume(string cause, string sparePart, DateTime? repairEnd = null)
    {
        double dt = Hive.CompleteRepairEnd(cause, sparePart, repairEnd);
        if (AlarmHistory.Count > 0 && AlarmHistory[0].ClearedByBadge == "ACTIVE")
        {
            AlarmHistory[0].ClearedByBadge = Hive.IsLoggedIn ? Hive.LoggedInBadgeName : "Operator";
            AlarmHistory[0].DowntimeMinutes = dt;
        }
        NoAlarm = true;
        _isEmoTest = false;
        OnPropertyChanged(nameof(IsEmoTest));
        ActiveAlarmCode = "No Alarm";
        ActiveAlarmCategory = "No Alarm";
        ActiveAlarmTime = "00:00:00";
        return dt;
    }

    private void SpawnProductInto(StationSlot slot, string prefix)
    {
        _serialCounter++;
        slot.No = _serialCounter;
        slot.SerialNo = $"{prefix}-V54-{_serialCounter:00000}";
        slot.WipCode = $"WIP-{Equipment.Site}-{_serialCounter:00000}";
        slot.HasProduct = true;
        slot.IsPass = true;
        slot.StepText = $"IN ({slot.Position})";
    }

    private void CompleteProduct(StationSlot slot)
    {
        Upd++;
        CycleTime = Math.Round(2.90 + _rng.NextDouble() * 0.75, 2);
        Uph = (int)Math.Round(3600.0 / Math.Max(CycleTime, 0.5));
        bool pass = _rng.NextDouble() > 0.04;
        if (pass) PassCount++; else NgCount++;

        int trayIdx = (Upd - 1) % TrayCells.Count;
        TrayCells[trayIdx].Used = true;
        if (trayIdx == TrayCells.Count - 1)
        {
            foreach (var t in TrayCells) t.Used = false;
            int casIdx = ((Upd / TrayCells.Count) - 1 + CassetteCells.Count) % CassetteCells.Count;
            CassetteCells[casIdx].Used = true;
        }

        double peakTorque = Math.Round(2.75 + (_rng.NextDouble() - 0.5) * 0.35, 2);
        LatestHistory.Insert(0, new ModuleHistoryItem
        {
            Timestamp = DateTime.Now.ToString("HH:mm:ss"),
            SerialNo = slot.SerialNo,
            Station = "HSG",
            CycleSec = CycleTime,
            PeakTorqueNm = peakTorque,
            Judge = pass ? "PASS" : "NG"
        });
        while (LatestHistory.Count > 12) LatestHistory.RemoveAt(LatestHistory.Count - 1);

        if (!pass)
        {
            TossingAndNgRecords.Insert(0, new TossingOrNgRecord
            {
                Time = DateTime.Now,
                Kind = "NG",
                Station = "HSG",
                SerialNo = slot.SerialNo,
                PartSn = $"MOD-{slot.No}",
                Reason = "Dynamic Vision T31 Offset > 0.035mm"
            });
        }

        int hr = DateTime.Now.Hour;
        HourlyCounts[hr]++;
        WeeklyCounts[(int)DateTime.Now.DayOfWeek]++;

        // Append live PDCA measurement points
        foreach (var item in PdcaItems)
        {
            double mid = (item.LSL + item.USL) / 2.0;
            double span = (item.USL - item.LSL) * 0.14;
            double val = Math.Round(mid + (_rng.NextDouble() - 0.5) * 2.0 * span, 4);
            item.DataPoints.Add(new PdcaDataPoint { TimeStamp = DateTime.Now, Value = val, SerialNo = slot.SerialNo });
            item.EndTime = DateTime.Now;
        }
    }

    private void GenerateScrewWaveform()
    {
        var pts = new List<(double Angle, double Torque)>(25);
        for (int i = 0; i <= 24; i++)
        {
            double angle = i * 150.0; // 0 .. 3600 deg
            double normalized = i / 24.0;
            double torque = normalized < 0.65
                ? 0.35 + normalized * 1.1 + (_rng.NextDouble() - 0.5) * 0.08
                : 1.06 + Math.Pow((normalized - 0.65) / 0.35, 1.8) * 1.82 + (_rng.NextDouble() - 0.5) * 0.06;
            pts.Add((angle, Math.Round(Math.Clamp(torque, 0, 4.8), 3)));
        }
        LastScrewId = $"GS{(_serialCounter % 2) + 1}-M1.6";
        LastWipCode = $"WIP-{Equipment.Site}-{_serialCounter:00000}";
        LastScrewCurve = pts;
    }

    private void IncrementSparePartsAndCheckAlarms()
    {
        foreach (var sp in SpareParts)
        {
            sp.CurrentCount++;
            if (sp.IsAlarmed && NoAlarm)
            {
                Storage.SaveSpareParts(SpareParts);
                RaiseAlarm(sp.AlarmCode, isHeavy: true);
                break;
            }
        }
    }

    private void SetIoBit(int addr, bool val)
    {
        var bit = IoBits.FirstOrDefault(b => b.Address == addr);
        if (bit is not null) bit.State = val;
    }

    private void SeedCollections()
    {
        (string Id, string Cat, string Port, bool Top)[] devs =
        [
            ("WMX3_ENGINE", "Motion", "RTX64-EtherCAT", true),
            ("AXTLibrary", "Motion", "PCIe-AXT", true),
            ("BCR", "AutoID", "192.168.1.50:23", true),
            ("HermesUpstream", "Hermes", "TCP:50100", true),
            ("HermesDownstream", "Hermes", "TCP:50101", false),
            ("BALIConnDevice", "MES", "10.0.0.10:5500", false),
            ("HIVE", "MES", $"{HiveCfg.ServerIp}:{HiveCfg.ServerPort}", false),
            ("SGML", "AI/ML", "Local-ONNX", false),
            ("KeyenceVisionIF", "Vision", "192.168.1.61:8500", false),
            ("CognexVisionIF", "Vision", "192.168.1.62:3000", false),
            ("CardReader", "Badge", "USB-HID-JRK6", false),
            ("RFID", "AutoID", "COM4-115200", false),
            ("ScrewDriver", "Actuator", "COM6-57600", false),
            ("WMX3_IO_SAMPLE", "MotionIO", "EtherCAT-Slave01", false),
            ("WMX3_MOTION_HEAD", "Motion", "Axis 0..5", false),
            ("WMX3_MOTION_STAGE", "Motion", "Axis 6..11", false),
            ("DiskCleaner", "Utility", @"D:\LogCleaner", false),
            ("MachineDataDB", "Database", "LocalJsonStore", false),
            ("CPKCollector", "SPC", "PDCA-Collector", false),
            ("MotorLoadDB", "Telemetry", "DataGravityHub", false)
        ];
        foreach (var d in devs)
            Devices.Add(new DeviceEntry { DeviceId = d.Id, Category = d.Cat, EndpointOrPort = d.Port, ShowInTopBar = d.Top, IsActivated = d.Id != "RFID" });

        foreach (var pos in new[] { "Loading", "Align", "Process", "Inspection", "Unloading" })
        {
            HsgSlots.Add(new StationSlot { Position = pos, StationGroup = 0 });
            StationMonitors.Add(new StationSlot { Position = pos, StationGroup = 0 });
        }
        foreach (var pos in new[] { "MPSLoading", "MPSProcess" })
            MpsSlots.Add(new StationSlot { Position = pos, StationGroup = 1 });

        for (int i = 0; i < 10; i++) CassetteCells.Add(new TrayCell { Index = i + 1, Label = $"C{i + 1:00}", Used = i < 3 });
        for (int i = 0; i < 12; i++) TrayCells.Add(new TrayCell { Index = i + 1, Label = $"T{i + 1:00}", Used = i < 5 });

        foreach (var a in Storage.LoadAlarmCatalog()) AlarmCatalog.Add(a);
        foreach (var s in Storage.LoadSpareParts()) SpareParts.Add(s);

        string[] axisNames = ["GSX1", "GSZ1", "GSX2", "GSZ2", "GSY1", "GSY2", "WCX1", "WCX2", "PUX", "PUY"];
        for (int i = 0; i < axisNames.Length; i++)
        {
            Axes.Add(new AxisStatus
            {
                AxisName = axisNames[i],
                MotionGroup = i < 6 ? "WMX3_MOTION_HEAD" : "WMX3_MOTION_STAGE",
                ActualPos = Math.Round(20.0 + i * 15.5, 2),
                CommandPos = Math.Round(20.0 + i * 15.5, 2),
                LoadPercent = Math.Round(18.0 + i * 3.1, 1)
            });
        }

        string[] inNames = ["DoorLock_Sw", "EMO_Loop_OK", "MainAir_OK", "HSG_InSensor", "HSG_AlignSensor", "HSG_ProcSensor", "HSG_OutSensor", "MPS_InSensor", "Hermes_UpReady", "Hermes_DnReady", "Screw1_TorqueOK", "Screw2_TorqueOK", "Vision_TrigAck", "LightCurtain_OK", "Ionizer_OK", "TrayPresent_Sw"];
        for (int i = 0; i < 16; i++)
            IoBits.Add(new IoBitItem { Address = i, Name = inNames[i], IsOutput = false, State = i is 0 or 1 or 2 or 13 or 14 or 15 });

        string[] outNames = ["Tower_Green", "Tower_Yellow", "Tower_Red", "Buzzer_On", "DoorSolenoid_Lock", "Vacuum_Pick1", "Vacuum_Pick2", "Clamp_HSG", "Clamp_MPS", "Screw1_Start", "Screw2_Start", "Vision_Strobe", "Conveyor_Run", "Ionizer_Blow", "Hermes_BoardAvail", "Reset_Driver"];
        for (int i = 0; i < 16; i++)
            IoBits.Add(new IoBitItem { Address = 100 + i, Name = outNames[i], IsOutput = true, State = i is 1 or 4 or 13 });

        BusopItems.Add(new BusopCheckItem { SopGroup = "Force", SequenceId = "Seq_Force_GantryScrew1", Lsl = 18.0, Usl = 22.0, Unit = "N", Measured = 19.8, Judge = "PASS" });
        BusopItems.Add(new BusopCheckItem { SopGroup = "Force", SequenceId = "Seq_Force_GantryScrew2", Lsl = 18.0, Usl = 22.0, Unit = "N", Measured = 20.1, Judge = "PASS" });
        BusopItems.Add(new BusopCheckItem { SopGroup = "LoadCell", SequenceId = "Seq_LoadCell_SOP", Lsl = 48.0, Usl = 52.0, Unit = "N", Measured = 50.2, Judge = "PASS" });
        BusopItems.Add(new BusopCheckItem { SopGroup = "VisionStatic", SequenceId = "Seq_T31_1_Static", Lsl = -0.015, Usl = 0.015, Unit = "mm", Measured = 0.003, Judge = "PASS" });
        BusopItems.Add(new BusopCheckItem { SopGroup = "VisionDynamic", SequenceId = "Seq_T31_2A_Dynamic_PostLoop", Lsl = -0.035, Usl = 0.035, Unit = "mm", Measured = -0.008, Judge = "PASS" });

        var baseStart = DateTime.Now.AddHours(-3);
        (string Name, string Unit, double Lsl, double Usl, double Center)[] specs =
        [
            ("HSG_Align_OffsetX", "mm", -0.050, 0.050, 0.004),
            ("HSG_Align_OffsetY", "mm", -0.050, 0.050, -0.003),
            ("Screw1_PeakTorque", "Nm", 2.50, 3.20, 2.86),
            ("Screw2_PeakTorque", "Nm", 2.50, 3.20, 2.84),
            ("Press_Force_Peak", "N", 45.0, 55.0, 50.1),
            ("MPS_Gap_Planarity", "mm", 0.05, 0.15, 0.098)
        ];
        foreach (var sp in specs)
        {
            var pts = new List<PdcaDataPoint>();
            double sigma = (sp.Usl - sp.Lsl) / 10.5;
            for (int k = 0; k < 36; k++)
            {
                double u1 = Math.Max(1e-6, _rng.NextDouble());
                double u2 = _rng.NextDouble();
                double z = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
                pts.Add(new PdcaDataPoint
                {
                    TimeStamp = baseStart.AddMinutes(k * 5),
                    Value = Math.Round(sp.Center + z * sigma, 4),
                    SerialNo = $"HSG-V54-{1000 + k:00000}"
                });
            }
            PdcaItems.Add(new PdcaMeasurementItem
            {
                Name = sp.Name,
                Unit = sp.Unit,
                LSL = sp.Lsl,
                USL = sp.Usl,
                StartTime = baseStart,
                EndTime = DateTime.Now,
                DataPoints = pts,
                IsShown = true
            });
        }

        string[] defaultLoggers = ["MainSequence", "HivePayload", "BaliClient", "WMX3Motion", "VisionIF", "ScrewDriver", "ButtonLogger", "AlarmService"];
        for (int i = 0; i < defaultLoggers.Length; i++)
            LogConfigs.Add(new LogConfigItem { SlotNo = i + 1, LoggerName = defaultLoggers[i], Level = "INFO", RetentionDays = 30, Enabled = true });

        GenerateScrewWaveform();
        Upd = 122;
        Uph = 1052;
        LatestHistory.Add(new ModuleHistoryItem { Timestamp = DateTime.Now.AddSeconds(-12).ToString("HH:mm:ss"), SerialNo = "HSG-V54-01041", Station = "HSG", CycleSec = 3.38, PeakTorqueNm = 2.86, Judge = "PASS" });
        LatestHistory.Add(new ModuleHistoryItem { Timestamp = DateTime.Now.AddSeconds(-25).ToString("HH:mm:ss"), SerialNo = "HSG-V54-01040", Station = "HSG", CycleSec = 3.44, PeakTorqueNm = 2.83, Judge = "PASS" });
        TossingAndNgRecords.Add(new TossingOrNgRecord { Time = DateTime.Now.AddMinutes(-28), Kind = "Tossing", Station = "MPS", SerialNo = "MPS-V54-00988", PartSn = "KEY-988", Reason = "Suction Nozzle Vacuum Drop Retry" });
        TossingAndNgRecords.Add(new TossingOrNgRecord { Time = DateTime.Now.AddMinutes(-44), Kind = "NG", Station = "HSG", SerialNo = "HSG-V54-00961", PartSn = "MOD-961", Reason = "Screw2 Floating Height Exceeded" });
    }
}
