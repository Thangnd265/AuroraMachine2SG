using System.IO.Compression;
using System.Text;
using System.Text.Json;
using AuroraMachine2SG.Core.Models;

namespace AuroraMachine2SG.Core.Services;

public sealed class StorageService
{
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    public string BasePath { get; }
    public string SettingDir => Path.Combine(BasePath, "_config_setting");
    public string RecipeDir => Path.Combine(BasePath, "_config_recipe");
    public string AlarmDir => Path.Combine(BasePath, "_alarm");
    public string ExportDir => Path.Combine(BasePath, "_export");
    public string BackupDir => Path.Combine(BasePath, "_backup");
    public string HiveLogDir => Path.Combine(BasePath, "HIVE", "log");
    public string EmulatorLogDir => Path.Combine(BasePath, "HIVE", "emulator_log");

    public StorageService(string basePath) => BasePath = basePath;

    public void EnsureSeeded()
    {
        foreach (var d in new[] { SettingDir, RecipeDir, AlarmDir, ExportDir, BackupDir, HiveLogDir, EmulatorLogDir })
            Directory.CreateDirectory(d);

        if (!File.Exists(Path.Combine(SettingDir, "Equipment.json")))
            SaveEquipment(new EquipmentConfig());
        if (!File.Exists(Path.Combine(SettingDir, "HiveSetting.json")))
            SaveHiveConfig(new HiveConfig());
        if (!File.Exists(Path.Combine(RecipeDir, "MainRecipe.json")))
            SaveMainRecipe(new MainRecipeData());
        if (!File.Exists(Path.Combine(RecipeDir, "WMX3PositionRecipe.json")))
            SaveTeachingRecipe(CreateDefaultTeaching());
        if (!File.Exists(Path.Combine(SettingDir, "SpareParts.json")))
            SaveSpareParts(CreateDefaultSpareParts());
        if (!File.Exists(Path.Combine(AlarmDir, "AlarmDescriptions.json")))
            WriteJson(Path.Combine(AlarmDir, "AlarmDescriptions.json"), CreateDefaultAlarmCatalog());
    }

    public EquipmentConfig LoadEquipment() => ReadJson(Path.Combine(SettingDir, "Equipment.json"), new EquipmentConfig());
    public void SaveEquipment(EquipmentConfig config) => WriteJson(Path.Combine(SettingDir, "Equipment.json"), config);

    public HiveConfig LoadHiveConfig() => ReadJson(Path.Combine(SettingDir, "HiveSetting.json"), new HiveConfig());
    public void SaveHiveConfig(HiveConfig config) => WriteJson(Path.Combine(SettingDir, "HiveSetting.json"), config);

    public MainRecipeData LoadMainRecipe() => ReadJson(Path.Combine(RecipeDir, "MainRecipe.json"), new MainRecipeData());
    public void SaveMainRecipe(MainRecipeData data) => WriteJson(Path.Combine(RecipeDir, "MainRecipe.json"), data);

    public TeachingRecipeData LoadTeachingRecipe() => ReadJson(Path.Combine(RecipeDir, "WMX3PositionRecipe.json"), CreateDefaultTeaching());
    public void SaveTeachingRecipe(TeachingRecipeData data) => WriteJson(Path.Combine(RecipeDir, "WMX3PositionRecipe.json"), data);

    public List<SparePartItem> LoadSpareParts() => ReadJson(Path.Combine(SettingDir, "SpareParts.json"), CreateDefaultSpareParts());
    public void SaveSpareParts(IEnumerable<SparePartItem> items) => WriteJson(Path.Combine(SettingDir, "SpareParts.json"), items.ToList());

    public List<AlarmCatalogEntry> LoadAlarmCatalog() => ReadJson(Path.Combine(AlarmDir, "AlarmDescriptions.json"), CreateDefaultAlarmCatalog());

    public void AppendHivePayload(HiveConfig cfg, string eventName, string jsonPayload)
    {
        Directory.CreateDirectory(HiveLogDir);
        var stamp = DateTime.Now.ToString("yyyy-MM-dd");
        var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{cfg.ServerIp}:{cfg.ServerPort}] {eventName} => {jsonPayload}{Environment.NewLine}";
        File.AppendAllText(Path.Combine(HiveLogDir, $"hive_{stamp}.log"), line, Encoding.UTF8);

        if (cfg.UseDualEmulatorLogging)
        {
            Directory.CreateDirectory(EmulatorLogDir);
            var emuLine = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [EMULATOR {cfg.EmulatorIp}:{cfg.EmulatorPort}] {eventName} => {jsonPayload}{Environment.NewLine}";
            File.AppendAllText(Path.Combine(EmulatorLogDir, $"hive_emulator_{stamp}.log"), emuLine, Encoding.UTF8);
        }
    }

    public string ExportCsv(string fileName, IEnumerable<string> headers, IEnumerable<string[]> rows)
    {
        Directory.CreateDirectory(ExportDir);
        var path = Path.Combine(ExportDir, fileName);
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",", headers.Select(EscapeCsv)));
        foreach (var row in rows)
            sb.AppendLine(string.Join(",", row.Select(EscapeCsv)));
        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        return path;
    }

    public string CreateBackupZip(string label = "auto")
    {
        Directory.CreateDirectory(BackupDir);
        var zipPath = Path.Combine(BackupDir, $"Backup_{label}_{DateTime.Now:yyyyMMdd_HHmmss}.zip");
        if (File.Exists(zipPath)) File.Delete(zipPath);

        using var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create);
        foreach (var dir in new[] { SettingDir, RecipeDir, AlarmDir })
        {
            if (!Directory.Exists(dir)) continue;
            var folderName = Path.GetFileName(dir);
            foreach (var file in Directory.GetFiles(dir))
                archive.CreateEntryFromFile(file, $"{folderName}/{Path.GetFileName(file)}");
        }
        return zipPath;
    }

    private static string EscapeCsv(string? s)
    {
        s ??= "";
        return (s.Contains(',') || s.Contains('"') || s.Contains('\n'))
            ? $"\"{s.Replace("\"", "\"\"")}\""
            : s;
    }

    private static T ReadJson<T>(string path, T fallback)
    {
        try
        {
            if (!File.Exists(path)) return fallback;
            return JsonSerializer.Deserialize<T>(File.ReadAllText(path, Encoding.UTF8), JsonOpts) ?? fallback;
        }
        catch
        {
            return fallback;
        }
    }

    private static void WriteJson<T>(string path, T data)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(data, JsonOpts), Encoding.UTF8);
    }

    public static TeachingRecipeData CreateDefaultTeaching()
    {
        string[] posNames = ["Loading", "Processing", "Unloading", "Align_Ready", "Align", "Align_Unload"];
        TeachingMotionCard MakeCard(string tab, string motion, double baseX) => new()
        {
            TabName = tab,
            MotionId = motion,
            Points = posNames.Select((p, idx) => new TeachingPoint
            {
                Name = p,
                TargetX = Math.Round(baseX + idx * 42.5, 2),
                TargetY = Math.Round(15.0 + idx * 18.2, 2),
                TargetZ = idx is 1 or 4 ? -12.50 : 0.00,
                Velocity = 200.0,
                Accel = 1500.0,
                Decel = 1500.0
            }).ToList()
        };

        return new TeachingRecipeData
        {
            TabNames = ["Stage_Main", "Head_Assy", "MPS_Sub"],
            MotionIds = ["WMX3_MOTION_STAGE", "WMX3_MOTION_HEAD", "WMX3_MOTION_TEACHINGSAMPLE"],
            Cards =
            [
                MakeCard("Stage_Main", "WMX3_MOTION_STAGE", 100.0),
                MakeCard("Head_Assy", "WMX3_MOTION_HEAD", 25.0),
                MakeCard("MPS_Sub", "WMX3_MOTION_TEACHINGSAMPLE", 60.0)
            ]
        };
    }

    public static List<SparePartItem> CreateDefaultSpareParts() =>
    [
        new() { Unit = "Vision", Name = "camera;相机", PartNumber = "SP-CAM-V54", IsUseAlarm = true, AlarmCode = 4101, CurrentCount = 3420, WarningLimit = 8000, MaxLimit = 10000 },
        new() { Unit = "GantryScrew", Name = "screw_bit;批头", PartNumber = "SP-BIT-M16", IsUseAlarm = true, AlarmCode = 4102, CurrentCount = 4890, WarningLimit = 4500, MaxLimit = 5000 },
        new() { Unit = "PickUp", Name = "suction_nozzle;吸嘴", PartNumber = "SP-NOZ-02", IsUseAlarm = true, AlarmCode = 4103, CurrentCount = 1250, WarningLimit = 7000, MaxLimit = 8000 },
        new() { Unit = "Conveyor", Name = "stopper_pad;阻挡垫", PartNumber = "SP-STP-09", IsUseAlarm = false, AlarmCode = 4104, CurrentCount = 910, WarningLimit = 15000, MaxLimit = 20000 }
    ];

    public static List<AlarmCatalogEntry> CreateDefaultAlarmCatalog() =>
    [
        new() { Code = 1001, Category = "Heavy", Unit = "Safety", DescEn = "Emergency Stop (EMO) Triggered", DescKo = "비상정지(EMO) 버튼이 눌렸습니다", DescZh = "急停开关(EMO)被触发", SolutionEn = "Release EMO switch, inspect hazard area, swipe badge and press Reset.", SolutionKo = "EMO 해제 후 작업 구역 확인 및 뱃지 스캔 후 리셋하십시오.", SolutionZh = "释放急停按钮，确认安全区域后刷卡复位。" },
        new() { Code = 2001, Category = "Heavy", Unit = "WMX3Motion", DescEn = "Stage Servo Following Error Exceeded", DescKo = "스테이지 서보 위치 추종 오차 초과", DescZh = "平台伺服跟随误差超限", SolutionEn = "Check mechanical jam on WCX1/WCX2, verify Servo On and re-home axis.", SolutionKo = "WCX1/WCX2 기구 간섭 확인 후 서보 온 및 원점 복귀 수행.", SolutionZh = "检查WCX1/WCX2机械干涉，重新使能并回原点。" },
        new() { Code = 3001, Category = "Light", Unit = "VisionIF", DescEn = "Align Fiducial Score Below Threshold", DescKo = "얼라인 마크 매칭 점수 미달", DescZh = "视觉对位特征分数低于阈值", SolutionEn = "Clean coaxial light & camera lens or adjust CameraRecipe exposure.", SolutionKo = "동축 조명 및 렌즈 청소 또는 카메라 노출값 조정.", SolutionZh = "清洁同轴光源与镜头，或调整相机曝光参数。" },
        new() { Code = 3050, Category = "Heavy", Unit = "ScrewDriver", DescEn = "Gantry Screw Torque Out of Spec (NG)", DescKo = "갠트리 스크류 체결 토크 규격 이탈", DescZh = "龙门电批锁附扭力超规(NG)", SolutionEn = "Inspect thread stripping / floating screw and check torque jig.", SolutionKo = "나사산 파손/들뜸 확인 및 토크 측정기 점검.", SolutionZh = "检查浮锁/滑牙并校验扭力计。" },
        new() { Code = 4101, Category = "Maintenance", Unit = "SpareParts", DescEn = "Camera Illumination Lifespan Reached", DescKo = "카메라 조명 수명 도달", DescZh = "相机光源寿命到期", SolutionEn = "Replace camera;相机 consumable and reset counter in SpareParts.", SolutionKo = "소모품 교체 후 카운터를 초기화하십시오.", SolutionZh = "更换相机光源并在易损件界面清零计数。" },
        new() { Code = 4102, Category = "Maintenance", Unit = "SpareParts", DescEn = "Screw Bit Lifespan Limit Exceeded", DescKo = "스크류 비트 교체 주기 도달", DescZh = "电批批头寿命超限", SolutionEn = "Replace screw_bit;批头 and verify BUSOP Force SOP.", SolutionKo = "스크류 비트 교체 후 BUSOP Force 점검 수행.", SolutionZh = "更换批头并执行BUSOP压力点检。" },
        new() { Code = 5001, Category = "Heavy", Unit = "HIVE", DescEn = "HIVE Server Heartbeat Communication Timeout", DescKo = "HIVE 서버 통신 응답 시간 초과", DescZh = "HIVE服务器心跳通信超时", SolutionEn = "Verify 10.0.0.2:5008 link and VendorStationInfo.ini onboarding.", SolutionKo = "10.0.0.2:5008 네트워크 및 온보딩 설정을 확인하십시오.", SolutionZh = "检查10.0.0.2:5008网络连接与站点注册文件。" }
    ];
}
