using AuroraMachine2SG.Core.Models;
using AuroraMachine2SG.Core.Services;
using AuroraMachine2SG.Core.ViewModels;

namespace AuroraMachine2SG.Tests;

public sealed class SpcAndMonitoringTests
{
    [Fact]
    public void SpcCalculator_ComputesExactCpCpkAndHandlesEdgeCases()
    {
        var t0 = new DateTime(2026, 11, 20, 8, 0, 0);
        var item = new PdcaMeasurementItem
        {
            Name = "Gap_X",
            LSL = 9.70,
            USL = 10.30,
            StartTime = t0,
            EndTime = t0.AddHours(0.5),
            DataPoints =
            [
                new() { TimeStamp = t0, Value = 9.95 },
                new() { TimeStamp = t0.AddMinutes(5), Value = 10.00 },
                new() { TimeStamp = t0.AddMinutes(10), Value = 10.05 },
                new() { TimeStamp = t0.AddMinutes(15), Value = 10.00 }
            ]
        };

        var res = SpcCalculator.Calculate(item, useSampleStd: true);
        AssertEx.Equal(4, res.N);
        AssertEx.Near(10.00, res.Mean, 1e-6);
        double expectedStd = Math.Sqrt((0.0025 + 0 + 0.0025 + 0) / 3.0);
        AssertEx.Near(expectedStd, res.StdDev, 1e-6);
        AssertEx.Near(0.60 / (6.0 * expectedStd), res.Cp, 1e-4);
        AssertEx.Near(0.30 / (3.0 * expectedStd), res.Cpk, 1e-4);
        AssertEx.Near(8.0, res.UPH, 1e-4);

        // Edge cases: empty & single sample & inverted spec
        var empty = SpcCalculator.Calculate(new PdcaMeasurementItem { LSL = 0, USL = 1 });
        AssertEx.Equal(0, empty.N);
        AssertEx.Near(0, empty.Cpk);

        var single = SpcCalculator.Calculate(new PdcaMeasurementItem
        {
            LSL = 0, USL = 10,
            DataPoints = [new() { TimeStamp = t0, Value = 5.0 }]
        });
        AssertEx.Equal(1, single.N);
        AssertEx.True(double.IsNaN(single.StdDev));
        AssertEx.Near(0, single.Cpk);
    }

    [Fact]
    public void MonitoringViewModel_FiltersItemsAndExportsCpkCsv()
    {
        var dir = Path.Combine(Path.GetTempPath(), "AuroraSG_Mon_" + Guid.NewGuid().ToString("N"));
        try
        {
            var storage = new StorageService(dir);
            storage.EnsureSeeded();
            var rt = new MachineRuntime(storage);
            rt.InitializeAllDevices(true);

            var vm = new MonitoringViewModel(rt);
            var list = vm.ApplySelectedItems(DateTime.Now.AddDays(-2), DateTime.Now.AddDays(1));
            AssertEx.True(list.Count > 0, "Selected PDCA items returned with SPC stats");

            var csv = vm.ExportCpkCsv(DateTime.Now.AddDays(-2), DateTime.Now.AddDays(1));
            AssertEx.True(File.Exists(csv), "CPK CSV file exported");
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }
    }
}
