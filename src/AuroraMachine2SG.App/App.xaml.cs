using System.IO;
using System.Windows;
using System.Windows.Threading;
using AuroraMachine2SG.Core.Models;
using AuroraMachine2SG.Core.Services;
using AuroraMachine2SG.Core.ViewModels;

namespace AuroraMachine2SG.App;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        string dataRoot = Path.Combine(AppContext.BaseDirectory, "_data");
        var storage = new StorageService(dataRoot);
        storage.EnsureSeeded();

        var runtime = new MachineRuntime(storage);
        runtime.InitializeAllDevices(true);
        var shellVm = new MainShellViewModel(runtime);

        bool smokeMode = e.Args.Length > 0 && e.Args[0] == "--smoke-test";
        var mainWin = new MainWindow(shellVm, skipStartupDialogs: smokeMode);
        MainWindow = mainWin;
        mainWin.Show();

        if (smokeMode)
        {
            string outDir = e.Args.Length > 1 ? e.Args[1] : storage.ExportDir;
            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(450) };
            int step = 0;
            timer.Tick += (_, _) =>
            {
                step++;
                if (step == 1)
                {
                    runtime.Hive.SwipeBadge("SMOKE-L8", "Wang Xiaoming", "PRI", PermissionLevel.Admin);
                    runtime.StartProduction();
                    for (int i = 0; i < 4; i++) runtime.StepSimulationCycle();
                    mainWin.CaptureWindowScreenshot(Path.Combine(outDir, "smoke_home_devtrace.png"));
                }
                else if (step == 2)
                {
                    shellVm.TryNavigate("Control", "Sequence");
                    if (shellVm.ControlVM.SelectedSequence?.Nodes.Count > 2)
                        shellVm.ControlVM.SelectedSequence.Nodes[2].HasBreakpoint = true;
                    mainWin.CaptureWindowScreenshot(Path.Combine(outDir, "smoke_sequence_flowchart.png"));
                }
                else if (step == 3)
                {
                    shellVm.TryNavigate("Control", "Device");
                    if (shellVm.ControlVM.SelectedDevice is { } dev)
                        shellVm.ControlVM.LastDeviceResponse = runtime.ExecuteSimulatedDeviceTest(dev, "TRIGGER_READ", "HSG-V54-99001", false);
                    mainWin.CaptureWindowScreenshot(Path.Combine(outDir, "smoke_device_testbench.png"));
                }
                else
                {
                    timer.Stop();
                    mainWin.Close();
                    Shutdown(0);
                }
            };
            timer.Start();
        }
    }
}
