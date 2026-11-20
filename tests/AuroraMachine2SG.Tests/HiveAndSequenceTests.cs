using AuroraMachine2SG.Core.Models;
using AuroraMachine2SG.Core.Services;
using AuroraMachine2SG.Core.ViewModels;

namespace AuroraMachine2SG.Tests;

public sealed class HiveAndSequenceTests
{
    private static (MachineRuntime Runtime, string TempDir) CreateIsolatedRuntime()
    {
        var dir = Path.Combine(Path.GetTempPath(), "AuroraSG_Hive_" + Guid.NewGuid().ToString("N"));
        var storage = new StorageService(dir);
        storage.EnsureSeeded();
        var rt = new MachineRuntime(storage);
        rt.InitializeAllDevices(true);
        return (rt, dir);
    }

    [Fact]
    public void HiveStateMachine_EnforcesQrOnDowntime_ChecklistCounterRule_AndRepairMinutes()
    {
        var (rt, dir) = CreateIsolatedRuntime();
        try
        {
            AssertEx.Equal(HiveState.Idle, rt.Hive.CurrentState);

            rt.Hive.EnterManualDowntime("Preventive Maintenance;设备保养");
            AssertEx.Equal(HiveState.PlannedDT, rt.Hive.CurrentState);
            AssertEx.True(rt.Hive.HasActiveQrCode, "QR code required in ManualDT per v2.6.6.0");

            // Partial checklist without badge must NOT reset weekly counter
            int beforeWeekly = rt.HiveCfg.WeeklyMaintRemainDays;
            rt.HiveCfg.WeeklyMaintRemainDays = 2;
            bool reset1 = rt.Hive.SubmitMaintenanceChecklist([true, false, true, true, true], isWeekly: true);
            AssertEx.False(reset1, "Partial checklist does not reset counter");
            AssertEx.Equal(2, rt.HiveCfg.WeeklyMaintRemainDays);

            // Swipe badge + 100% checked resets weekly counter to 7
            rt.Hive.SwipeBadge("BDG-008", "Wang Xiaoming", "PRI", PermissionLevel.Admin);
            bool reset2 = rt.Hive.SubmitMaintenanceChecklist([true, true, true, true, true], isWeekly: true);
            AssertEx.True(reset2, "Full checklist after badge resets counter");
            AssertEx.Equal(7, rt.HiveCfg.WeeklyMaintRemainDays);

            // Auto-triggered alarm -> RepairEnd downtime calculation (v2.6.6.4)
            var t0 = new DateTime(2026, 11, 20, 8, 0, 0);
            var t1 = t0.AddMinutes(12.5);
            rt.Hive.TriggerAutoAlarmDowntime(2001, "Servo Error", t0);
            AssertEx.Equal(HiveState.UnplannedDT, rt.Hive.CurrentState);
            AssertEx.True(rt.Hive.HasActiveQrCode, "QR code required on Auto-Triggered Alarm");

            double dtMin = rt.Hive.CompleteRepairEnd("Servo Jam;伺服卡阻", "screw_bit;批头", t1);
            AssertEx.Near(12.5, dtMin, 0.01, "Downtime minutes = RepairEnd - HelpRequest");
            AssertEx.Equal(HiveState.Idle, rt.Hive.CurrentState);
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void MachineRuntime_EnforcesRunningInterlocks_AdvancesStations_AndGatesProtectedMenus()
    {
        var (rt, dir) = CreateIsolatedRuntime();
        try
        {
            var shell = new MainShellViewModel(rt);

            // Protected menu blocked when logged out
            AssertEx.False(shell.TryNavigate("Control", "Manual"), "Control requires LEVEL5+");
            AssertEx.Equal("Main", shell.ActiveMainMenu);

            rt.Hive.SwipeBadge("ENG-501", "Kim Engineer", "LGPRI", PermissionLevel.Engineer);
            AssertEx.True(shell.TryNavigate("Control", "Manual"), "Engineer L5 accesses Control");
            AssertEx.Equal("Control", shell.ActiveMainMenu);

            // Start production -> interlocks block Door Open and Mode switch
            AssertEx.True(rt.StartProduction(), "Start succeeds with door locked");
            AssertEx.Equal(HiveState.Running, rt.Hive.CurrentState);
            AssertEx.False(rt.TrySetDoorLocked(false), "Cannot unlock door while sequence running");
            AssertEx.False(rt.TryChangeMode(MachineMode.DRYRUN), "Cannot change mode while sequence running");

            // Step simulation enough ticks to complete products through 5 HSG stages
            for (int i = 0; i < 8; i++)
                rt.StepSimulationCycle();

            AssertEx.True(rt.Upd > 0, "UPD incremented after completing pipeline");
            AssertEx.True(rt.Uph > 0, "UPH calculated");
            AssertEx.True(rt.LatestHistory.Count > 0, "LatestHistory populated");
            AssertEx.True(rt.LastScrewCurve.Count > 5, "Screw torque curve generated");

            // RemoveProduct safe on populated and empty slots
            var slot = rt.StationMonitors.First();
            slot.HasProduct = true;
            rt.RemoveProductFromSlot(slot);
            AssertEx.False(slot.HasProduct, "Product removed");
            rt.RemoveProductFromSlot(slot); // second call on empty slot must not throw

            rt.StopProduction();
            AssertEx.True(rt.TrySetDoorLocked(false), "Door opens once stopped");
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }
    }
}
