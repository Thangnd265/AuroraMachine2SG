using AuroraMachine2SG.Core.Services;

namespace AuroraMachine2SG.Tests;

public sealed class FoundationTests
{
    [Fact]
    public void QrMatrixGenerator_Produces21x21FinderPatterns_AndChangesWithPayload()
    {
        var m1 = QrMatrixGenerator.Generate("HIVE|RSMS-001|IDLE|0");
        var m2 = QrMatrixGenerator.Generate("HIVE|RSMS-001|DOWN|4101");

        AssertEx.Equal(21, m1.GetLength(0), "Rows");
        AssertEx.Equal(21, m1.GetLength(1), "Cols");

        // Top-left, top-right, bottom-left finder corners must be dark (true)
        AssertEx.True(m1[0, 0] && m1[0, 6] && m1[6, 0] && m1[6, 6] && m1[3, 3], "Top-left finder");
        AssertEx.True(m1[0, 14] && m1[0, 20] && m1[6, 20] && m1[3, 17], "Top-right finder");
        AssertEx.True(m1[14, 0] && m1[20, 0] && m1[20, 6] && m1[17, 3], "Bottom-left finder");

        bool anyDiff = false;
        for (int r = 0; r < 21; r++)
            for (int c = 0; c < 21; c++)
                if (m1[r, c] != m2[r, c]) anyDiff = true;
        AssertEx.True(anyDiff, "Different HIVE payloads must produce distinct QR data modules");
    }

    [Fact]
    public void LocalizationService_SwitchesBetweenFourLanguages_WithSafeFallback()
    {
        var loc = new LocalizationService();
        AssertEx.Equal("en-US", loc.CurrentLanguage);
        AssertEx.Equal("Main", loc["Main"]);

        loc.SetLanguage("ko-KR");
        AssertEx.Equal("ko-KR", loc.CurrentLanguage);
        AssertEx.Equal("메인", loc["Main"]);

        loc.SetLanguage("zh-CN");
        AssertEx.Equal("主界面", loc["Main"]);

        loc.SetLanguage("hi-IN");
        AssertEx.Equal("मुख्य", loc["Main"]);

        AssertEx.Equal("UnknownTokenXYZ", loc["UnknownTokenXYZ"]);
    }

    [Fact]
    public void StorageService_SeedsDefaults_PersistsJson_WritesDualHiveLogs_ExportsCsv_AndCreatesBackupZip()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "AuroraSG_Test_" + Guid.NewGuid().ToString("N"));
        try
        {
            var storage = new StorageService(tempDir);
            storage.EnsureSeeded();

            var equip = storage.LoadEquipment();
            AssertEx.Equal("RSMS-001", equip.EquipmentName);
            equip.Site = "TEMD";
            storage.SaveEquipment(equip);
            AssertEx.Equal("TEMD", storage.LoadEquipment().Site);

            var hiveCfg = storage.LoadHiveConfig();
            AssertEx.True(hiveCfg.UseDualEmulatorLogging);
            storage.AppendHivePayload(hiveCfg, "machine_state", "{\"state\":2}");

            var hiveLogs = Directory.GetFiles(Path.Combine(tempDir, "HIVE", "log"), "*.log");
            var emuLogs = Directory.GetFiles(Path.Combine(tempDir, "HIVE", "emulator_log"), "*.log");
            AssertEx.True(hiveLogs.Length > 0, "Primary HIVE log written");
            AssertEx.True(emuLogs.Length > 0, "Dual Emulator HIVE log written");

            var csvPath = storage.ExportCsv("cpk_test.csv", ["Item", "Cpk"], [["GapX", "1.67"]]);
            AssertEx.True(File.Exists(csvPath), "CSV file exported");
            AssertEx.True(File.ReadAllText(csvPath).Contains("GapX,1.67"), "CSV contents verified");

            var zipPath = storage.CreateBackupZip("unit_test");
            AssertEx.True(File.Exists(zipPath), "Backup ZIP created");
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }
}
