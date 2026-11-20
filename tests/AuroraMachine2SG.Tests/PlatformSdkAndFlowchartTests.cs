using AuroraMachine2SG.Core.Sdk;
using AuroraMachine2SG.Core.Services;

namespace AuroraMachine2SG.Tests;

public sealed class PlatformSdkAndFlowchartTests
{
    [Fact]
    public void SequenceBuilder_ExecutesActionDecisionEventAcceptor_Breakpoints_AndForceError()
    {
        var log = new HisLoggerHub();
        var ctx = new SequenceContext(log);
        string? forceErrNode = null;

        var runner = new SequenceBuilder("CustomLoadingSeq")
            .AddAction("INIT", c => { c.StateBag["count"] = 1; return true; })
            .AddDecision("CHECK_MODE", (c, branches) => c.PendingEvents.Contains("AUTO") ? "CHECK_BCR" : "WAIT_MANUAL")
            .AddEventAcceptor("WAIT_MANUAL", "EVENT_MANUAL_LOADING", c => c.StateBag["manual"] = true)
            .AddAction("CHECK_BCR", _ => true)
            .AddDecision("CHECK_STOP", (c, _) => c.IsCycleStopRequested ? "COMPLETE" : "INIT")
            .AddAction("COMPLETE", _ => true)
            .Flow("Init", "INIT", "CHECK_MODE", "CHECK_BCR", "CHECK_STOP", "COMPLETE", "Final")
            .Flow("CHECK_MODE", "WAIT_MANUAL", "CHECK_BCR")
            .Flow("CHECK_STOP", "INIT")
            .OnForceError((_, nodeId) => forceErrNode = nodeId)
            .Build(ctx);

        // Step Init -> INIT -> CHECK_MODE -> WAIT_MANUAL (blocks until event published!)
        runner.StepOneNode(); // Init -> INIT
        AssertEx.Equal("INIT", runner.CurrentNodeId);
        runner.StepOneNode(); // executes INIT -> moves to CHECK_MODE
        AssertEx.Equal("CHECK_MODE", runner.CurrentNodeId);
        runner.StepOneNode(); // decides WAIT_MANUAL
        AssertEx.Equal("WAIT_MANUAL", runner.CurrentNodeId);

        // Without event, stepping WAIT_MANUAL stays waiting
        runner.StepOneNode();
        AssertEx.Equal("WAIT_MANUAL", runner.CurrentNodeId);
        AssertEx.Equal(SequenceNodeState.WaitingEvent, runner.Nodes.First(n => n.Id == "WAIT_MANUAL").State);

        // Publish EVENT_MANUAL_LOADING -> advances to CHECK_BCR
        ctx.PendingEvents.Add("EVENT_MANUAL_LOADING");
        runner.StepOneNode();
        AssertEx.Equal("CHECK_BCR", runner.CurrentNodeId);

        // Arm ForceError on CHECK_BCR -> triggers OnForceError & halts
        runner.Nodes.First(n => n.Id == "CHECK_BCR").ForceErrorArmed = true;
        runner.StepOneNode();
        AssertEx.True(runner.IsHaltedOnError, "Sequence halted on ForceError");
        AssertEx.Equal("CHECK_BCR", forceErrNode);
        AssertEx.True(log.Entries.Count > 0, "HisLogger recorded node trace");
    }

    [Fact]
    public void BuiltInProfiles_SwitchCleanly_IncludingMultiFixture4Bay_AndCpkSimulatorStaysInBounds()
    {
        var profiles = BuiltInEquipmentProfiles.GetAll();
        AssertEx.True(profiles.Count >= 3, "At least 3 extensible machine profiles shipped");

        var dir = Path.Combine(Path.GetTempPath(), "AuroraSG_Plat_" + Guid.NewGuid().ToString("N"));
        try
        {
            var storage = new StorageService(dir);
            storage.EnsureSeeded();
            var rt = new MachineRuntime(storage);

            // Switch to MultiFixture (BAY1..BAY4)
            var multiFix = profiles.First(p => p.Topology == MachineTopology.MultiFixture);
            rt.LoadEquipmentProfile(multiFix);
            AssertEx.True(rt.ActiveSequences.Any(s => s.SequenceId.Contains("BAY1")), "BAY1 sequence loaded");
            AssertEx.True(rt.ActiveSequences.Any(s => s.SequenceId.Contains("BAY4")), "BAY4 sequence loaded");

            // Switch to Custom Starter Template
            var starter = profiles.First(p => p.ProfileId.Contains("Custom"));
            rt.LoadEquipmentProfile(starter);
            AssertEx.Equal(starter.EquipmentName, rt.Equipment.EquipmentName);

            // Verify Ornstein-Uhlenbeck CpkSignalSimulator stays reflected within LSL..USL
            var sim = new CpkSignalSimulator(mean: 10.0, lsl: 9.8, usl: 10.2, sigma: 0.02, seed: 42);
            for (int i = 0; i < 100; i++)
            {
                double v = sim.Next();
                AssertEx.True(v >= 9.8 && v <= 10.2, $"Simulated value {v} within [9.8, 10.2]");
            }
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }
    }
}
