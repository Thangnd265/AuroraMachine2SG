using AuroraMachine2SG.Core.Models;
using AuroraMachine2SG.Core.ViewModels;

namespace AuroraMachine2SG.Core.Services;

public sealed class HiveStateMachine : ObservableObject
{
    private readonly HiveConfig _config;
    private readonly StorageService _storage;

    private HiveState _currentState = HiveState.Idle;
    private bool _hasActiveQrCode;
    private string _activeQrPayload = "HIVE|RSMS-001|IDLE";
    private DateTime? _helpRequestTime;
    private double _lastDowntimeMinutes;
    private string _loggedInBadgeId = "";
    private string _loggedInBadgeName = "Guest";
    private string _loggedInVendor = "NONE";
    private PermissionLevel _currentPermission = PermissionLevel.NONE;

    public HiveStateMachine(HiveConfig config, StorageService storage)
    {
        _config = config;
        _storage = storage;
        RefreshQr("IDLE");
    }

    public HiveState CurrentState
    {
        get => _currentState;
        private set
        {
            if (SetProperty(ref _currentState, value))
                OnPropertyChanged(nameof(StateDisplayText));
        }
    }

    public string StateDisplayText => $"{CurrentState}";

    public bool HasActiveQrCode
    {
        get => _hasActiveQrCode;
        private set => SetProperty(ref _hasActiveQrCode, value);
    }

    public string ActiveQrPayload
    {
        get => _activeQrPayload;
        private set
        {
            if (SetProperty(ref _activeQrPayload, value))
                OnPropertyChanged(nameof(QrMatrix));
        }
    }

    public bool[,] QrMatrix => QrMatrixGenerator.Generate(_activeQrPayload);

    public DateTime? HelpRequestTime { get => _helpRequestTime; private set => SetProperty(ref _helpRequestTime, value); }
    public double LastDowntimeMinutes { get => _lastDowntimeMinutes; private set => SetProperty(ref _lastDowntimeMinutes, value); }
    public string LoggedInBadgeName { get => _loggedInBadgeName; private set => SetProperty(ref _loggedInBadgeName, value); }
    public string LoggedInVendor { get => _loggedInVendor; private set => SetProperty(ref _loggedInVendor, value); }
    public PermissionLevel CurrentPermission
    {
        get => _currentPermission;
        private set
        {
            if (SetProperty(ref _currentPermission, value))
                OnPropertyChanged(nameof(IsLoggedIn));
        }
    }

    public bool IsLoggedIn => CurrentPermission > PermissionLevel.NONE;

    public bool SwipeBadge(string badgeId, string name, string vendor, PermissionLevel level)
    {
        if (string.IsNullOrWhiteSpace(badgeId)) return false;
        _loggedInBadgeId = badgeId;
        LoggedInBadgeName = name;
        LoggedInVendor = vendor;
        CurrentPermission = level;
        _storage.AppendHivePayload(_config, "badge_scan",
            $"{{\"machine_sn\":\"{_config.MachineSn}\",\"badge_id\":\"{badgeId}\",\"name\":\"{name}\",\"vendor\":\"{vendor}\",\"level\":\"{level}\",\"scan_time\":\"{DateTime.Now:O}\"}}");
        return true;
    }

    public void Logout()
    {
        _loggedInBadgeId = "";
        LoggedInBadgeName = "Guest";
        LoggedInVendor = "NONE";
        CurrentPermission = PermissionLevel.NONE;
    }

    public void SetRunning()
    {
        CurrentState = HiveState.Running;
        HasActiveQrCode = false;
        RefreshQr("RUNNING");
        _storage.AppendHivePayload(_config, "machine_state", "{\"state\":1,\"name\":\"Running\"}");
    }

    public void SetIdle()
    {
        CurrentState = HiveState.Idle;
        HasActiveQrCode = false;
        RefreshQr("IDLE");
        _storage.AppendHivePayload(_config, "machine_state", "{\"state\":2,\"name\":\"Idle\"}");
    }

    public void SetEngineering()
    {
        CurrentState = HiveState.Engineering;
        HasActiveQrCode = true;
        RefreshQr("ENGINEERING");
        _storage.AppendHivePayload(_config, "machine_state", "{\"state\":3,\"name\":\"Engineering\"}");
    }

    public void SetDegraded(string stationInfo)
    {
        CurrentState = HiveState.Degraded;
        HasActiveQrCode = true;
        RefreshQr($"DEGRADED|{stationInfo}");
        _storage.AppendHivePayload(_config, "machine_state", $"{{\"state\":6,\"name\":\"Degraded\",\"station\":\"{stationInfo}\"}}");
    }

    public void EnterManualDowntime(string bilingualCause)
    {
        HelpRequestTime = DateTime.Now;
        CurrentState = HiveState.PlannedDT;
        HasActiveQrCode = true;
        RefreshQr($"MANUAL_DT|{bilingualCause}|{HelpRequestTime:HHmmss}");
        _storage.AppendHivePayload(_config, "manual_dt", $"{{\"state\":4,\"cause\":\"{bilingualCause}\",\"help_request\":\"{HelpRequestTime:O}\"}}");
    }

    public void TriggerAutoAlarmDowntime(int alarmCode, string description, DateTime? requestTime = null)
    {
        HelpRequestTime = requestTime ?? DateTime.Now;
        CurrentState = HiveState.UnplannedDT;
        HasActiveQrCode = true;
        RefreshQr($"ALARM_DT|{alarmCode}|{description}|{HelpRequestTime:HHmmss}");
        _storage.AppendHivePayload(_config, "auto_trigger_alarm", $"{{\"state\":5,\"code\":{alarmCode},\"desc\":\"{description}\",\"help_request\":\"{HelpRequestTime:O}\"}}");
    }

    public bool SubmitMaintenanceChecklist(IReadOnlyList<bool> checkedFlags, bool isWeekly = true)
    {
        // v2.6.6.0 rule: Submit is ALWAYS allowed, but counter updates ONLY when badge is scanned AND all items are checked
        bool allChecked = checkedFlags.Count > 0 && checkedFlags.All(x => x);
        bool canResetCounter = IsLoggedIn && allChecked;

        if (canResetCounter)
        {
            if (isWeekly) _config.WeeklyMaintRemainDays = 7;
            else _config.MonthlyMaintRemainDays = 30;
            _storage.SaveHiveConfig(_config);
        }

        _storage.AppendHivePayload(_config, "maint_checklist_submit",
            $"{{\"badge\":\"{_loggedInBadgeId}\",\"all_checked\":{allChecked.ToString().ToLowerInvariant()},\"counter_reset\":{canResetCounter.ToString().ToLowerInvariant()}}}");
        return canResetCounter;
    }

    public double CompleteRepairEnd(string bilingualCause, string bilingualSparePart, DateTime? repairEndTime = null)
    {
        var end = repairEndTime ?? DateTime.Now;
        var start = HelpRequestTime ?? end;
        double minutes = Math.Max(0.0, Math.Round((end - start).TotalMinutes, 2));
        LastDowntimeMinutes = minutes;
        HelpRequestTime = null;
        CurrentState = HiveState.Idle;
        HasActiveQrCode = false;
        RefreshQr($"REPAIR_END|{minutes:F2}m");

        _storage.AppendHivePayload(_config, "repair_end",
            $"{{\"cause\":\"{bilingualCause}\",\"spare_part\":\"{bilingualSparePart}\",\"downtime_min\":{minutes:F2},\"badge\":\"{_loggedInBadgeId}\"}}");
        return minutes;
    }

    private void RefreshQr(string tag)
        => ActiveQrPayload = $"HIVE3|{_config.MachineSn}|{tag}|{DateTime.Now:yyyyMMddHHmmss}";
}
