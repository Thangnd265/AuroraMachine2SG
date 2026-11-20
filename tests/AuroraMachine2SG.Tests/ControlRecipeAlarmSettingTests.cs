using AuroraMachine2SG.Core.Services;
using AuroraMachine2SG.Core.ViewModels;

namespace AuroraMachine2SG.Tests;

public sealed class ControlRecipeAlarmSettingTests
{
    [Fact]
    public void ControlAndRecipe_JogAxes_ValidateBusop_AndPersistClonedTeachingCards()
    {
        var dir = Path.Combine(Path.GetTempPath(), "AuroraSG_Ctrl_" + Guid.NewGuid().ToString("N"));
        try
        {
            var storage = new StorageService(dir);
            storage.EnsureSeeded();
            var rt = new MachineRuntime(storage);
            rt.InitializeAllDevices(true);

            var ctrl = new ControlViewModel(rt);
            var firstAxis = rt.Axes.First();
            double p0 = firstAxis.ActualPos;
            ctrl.JogAxis(firstAxis.AxisName, 12.5);
            AssertEx.Near(p0 + 12.5, firstAxis.ActualPos, 1e-4);

            var sop = rt.BusopItems.First();
            double mid = (sop.Lsl + sop.Usl) / 2.0;
            AssertEx.Equal("PASS", ctrl.RunBusopCheck(sop, mid));
            AssertEx.Equal("NG", ctrl.RunBusopCheck(sop, sop.Usl + 50.0));

            var recipe = new RecipeViewModel(rt);
            recipe.AddTab("Calib_Tab");
            var srcCard = recipe.TeachingData.Cards.First();
            srcCard.Points[0].TargetX = 777.25;
            recipe.CloneCardToTab(srcCard, "Calib_Tab");
            recipe.SaveAll();

            var reloaded = storage.LoadTeachingRecipe();
            var cloned = reloaded.Cards.FirstOrDefault(c => c.TabName == "Calib_Tab");
            AssertEx.NotNull(cloned, "Cloned card persisted");
            AssertEx.Near(777.25, cloned!.Points[0].TargetX, 1e-4);
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void AlarmAndSetting_TriggersSparePartAlarm_GeneratesConstCode_AndCaps20LogConfigs()
    {
        var dir = Path.Combine(Path.GetTempPath(), "AuroraSG_Set_" + Guid.NewGuid().ToString("N"));
        try
        {
            var storage = new StorageService(dir);
            storage.EnsureSeeded();
            var rt = new MachineRuntime(storage);
            rt.InitializeAllDevices(true);

            var alarmVm = new AlarmViewModel(rt);
            AssertEx.True(alarmVm.SearchCatalog("비상정지").Count > 0, "Korean alarm search works");
            AssertEx.Equal(0, alarmVm.ValidateAlarmStandard().Count, "Default catalog passes HIVE validation");

            var setVm = new SettingViewModel(rt);
            var generated = setVm.GenerateSparePartNamesCode();
            AssertEx.True(generated.Contains("public static class SparePartNames"), "String generator creates C# const class");

            // Cap log configs at 20 per v2.6.5.1
            while (rt.LogConfigs.Count < 20)
                AssertEx.True(setVm.TryAddLogConfig($"Logger_{rt.LogConfigs.Count + 1}", "INFO"));
            AssertEx.False(setVm.TryAddLogConfig("Logger_21_Overflow", "DEBUG"), "21st log config rejected per v2.6.5.1");

            // Spare part threshold alarm trigger during production cycle
            var screwBit = rt.SpareParts.First(s => s.AlarmCode == 4102);
            screwBit.CurrentCount = screwBit.MaxLimit - 1;
            rt.StartProduction();
            rt.StepSimulationCycle();
            AssertEx.False(rt.NoAlarm, "Spare part reaching MaxLimit triggers configured alarm");
            AssertEx.Equal("4102", rt.ActiveAlarmCode);
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }
    }
}
