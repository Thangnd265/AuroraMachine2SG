using AuroraMachine2SG.Core.ViewModels;

namespace AuroraMachine2SG.Core.Services;

public sealed class LocalizationService : ObservableObject
{
    public static readonly string[] SupportedLanguages = ["en-US", "ko-KR", "zh-CN", "hi-IN"];

    private static readonly Dictionary<string, Dictionary<string, string>> Table = new(StringComparer.OrdinalIgnoreCase)
    {
        ["en-US"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Main"] = "Main",
            ["Control"] = "Control",
            ["Recipe"] = "Recipe",
            ["Alarm"] = "Alarm",
            ["Monitoring"] = "Monitoring",
            ["Setting"] = "Setting",
            ["Weekly"] = "Weekly",
            ["Monthly"] = "Monthly",
            ["Play"] = "Play",
            ["Pause"] = "Pause",
            ["Stop"] = "Stop",
            ["DoorOpen"] = "Door Open",
            ["DoorLock"] = "Door Lock",
            ["ManualLoad"] = "Manual Load",
            ["ManualUnload"] = "Manual UnLoad",
            ["CycleTime"] = "Cycle Time",
            ["LatestHistory"] = "Latest History",
            ["Login"] = "Login",
            ["Logout"] = "Logout",
            ["SpareParts"] = "SpareParts",
            ["PerformanceMetrics"] = "Performance Metrics",
            ["ExportCSV"] = "Export CSV",
            ["Today"] = "Today",
            ["All"] = "All",
            ["ApplySelectedItems"] = "Apply Selected Items"
        },
        ["ko-KR"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Main"] = "메인",
            ["Control"] = "제어",
            ["Recipe"] = "레시피",
            ["Alarm"] = "알람",
            ["Monitoring"] = "모니터링",
            ["Setting"] = "설정",
            ["Weekly"] = "주간점검",
            ["Monthly"] = "월간점검",
            ["Play"] = "시작",
            ["Pause"] = "일시정지",
            ["Stop"] = "정지",
            ["DoorOpen"] = "도어 열림",
            ["DoorLock"] = "도어 잠금",
            ["ManualLoad"] = "수동 투입",
            ["ManualUnload"] = "수동 배출",
            ["CycleTime"] = "사이클 타임",
            ["LatestHistory"] = "최근 생산이력",
            ["Login"] = "로그인",
            ["Logout"] = "로그아웃",
            ["SpareParts"] = "소모품 관리",
            ["PerformanceMetrics"] = "가동률 지표",
            ["ExportCSV"] = "CSV 내보내기",
            ["Today"] = "오늘",
            ["All"] = "전체",
            ["ApplySelectedItems"] = "선택 항목 적용"
        },
        ["zh-CN"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Main"] = "主界面",
            ["Control"] = "控制",
            ["Recipe"] = "配方",
            ["Alarm"] = "报警",
            ["Monitoring"] = "监控",
            ["Setting"] = "设置",
            ["Weekly"] = "周保养",
            ["Monthly"] = "月保养",
            ["Play"] = "启动",
            ["Pause"] = "暂停",
            ["Stop"] = "停止",
            ["DoorOpen"] = "开门",
            ["DoorLock"] = "锁门",
            ["ManualLoad"] = "手动上料",
            ["ManualUnload"] = "手动下料",
            ["CycleTime"] = "节拍时间",
            ["LatestHistory"] = "最近生产记录",
            ["Login"] = "登录",
            ["Logout"] = "注销",
            ["SpareParts"] = "易损件管理",
            ["PerformanceMetrics"] = "设备效率指标",
            ["ExportCSV"] = "导出 CSV",
            ["Today"] = "今天",
            ["All"] = "全部",
            ["ApplySelectedItems"] = "应用所选项目"
        },
        ["hi-IN"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Main"] = "मुख्य",
            ["Control"] = "नियंत्रण",
            ["Recipe"] = "रेसिपी",
            ["Alarm"] = "अलार्म",
            ["Monitoring"] = "निगरानी",
            ["Setting"] = "सेटिंग",
            ["Weekly"] = "साप्ताहिक",
            ["Monthly"] = "मासिक",
            ["Play"] = "चलाएँ",
            ["Pause"] = "रोकें",
            ["Stop"] = "बंद करें",
            ["DoorOpen"] = "दरवाज़ा खोलें",
            ["DoorLock"] = "दरवाज़ा लॉक",
            ["ManualLoad"] = "मैनुअल लोड",
            ["ManualUnload"] = "मैनुअल अनलोड",
            ["CycleTime"] = "साइकिल समय",
            ["LatestHistory"] = "नवीनतम इतिहास",
            ["Login"] = "लॉगिन",
            ["Logout"] = "लॉगआउट",
            ["SpareParts"] = "स्पेयर पार्ट्स",
            ["PerformanceMetrics"] = "प्रदर्शन मेट्रिक्स",
            ["ExportCSV"] = "CSV निर्यात",
            ["Today"] = "आज",
            ["All"] = "सभी",
            ["ApplySelectedItems"] = "चयनित लागू करें"
        }
    };

    private string _currentLanguage = "en-US";

    public string CurrentLanguage
    {
        get => _currentLanguage;
        private set => SetProperty(ref _currentLanguage, value);
    }

    public void SetLanguage(string cultureCode)
    {
        if (!Table.ContainsKey(cultureCode)) cultureCode = "en-US";
        CurrentLanguage = cultureCode;
        OnPropertyChanged("Item[]");
    }

    public string this[string key]
    {
        get
        {
            if (Table.TryGetValue(_currentLanguage, out var dict) && dict.TryGetValue(key, out var val))
                return val;
            if (Table["en-US"].TryGetValue(key, out var fallback))
                return fallback;
            return key;
        }
    }
}
