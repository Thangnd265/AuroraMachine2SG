using System.Collections.ObjectModel;
using System.Diagnostics;
using AuroraMachine2SG.Core.ViewModels;

namespace AuroraMachine2SG.Core.Sdk;

public enum SequenceNodeKind { Init, Action, Decision, EventAcceptor, Final }
public enum SequenceNodeState { Idle, Running, Completed, WaitingEvent, PausedAtBreakpoint, Error }

public sealed class LogRecord
{
    public DateTime Time { get; init; } = DateTime.Now;
    public string Level { get; init; } = "INFO";
    public string Category { get; init; } = "";
    public string Message { get; init; } = "";
    public string Formatted => $"[{Time:HH:mm:ss.fff}] [{Level,-5}] [{Category,-14}] {Message}";
}

public sealed class HisLoggerHub : ObservableObject
{
    private string _latestLine = "[READY] Aurora Simulation & Dev Trace Initialized.";
    public ObservableCollection<LogRecord> Entries { get; } = [];

    public string LatestLine
    {
        get => _latestLine;
        private set => SetProperty(ref _latestLine, value);
    }

    public void Log(string level, string category, string message)
    {
        var rec = new LogRecord { Time = DateTime.Now, Level = level, Category = category, Message = message };
        Entries.Insert(0, rec);
        while (Entries.Count > 400)
            Entries.RemoveAt(Entries.Count - 1);
        LatestLine = rec.Formatted;
    }

    public void Info(string category, string message) => Log("INFO", category, message);
    public void Warn(string category, string message) => Log("WARN", category, message);
    public void Error(string category, string message) => Log("ERROR", category, message);
    public void Clear()
    {
        Entries.Clear();
        LatestLine = "[CLEARED] Log buffer cleared.";
    }
}

/// <summary>
/// Ported from SGMachine.Sequences.ItemSignal (Ornstein-Uhlenbeck mean-reverting + outlier + boundary reflection).
/// </summary>
public sealed class CpkSignalSimulator
{
    private readonly Random _rng;
    private readonly double _mean, _lsl, _usl, _sigma, _kappa, _outlierProb;
    private double _x;

    public CpkSignalSimulator(double mean, double lsl, double usl, double sigma, double kappa = 0.15, double outlierProb = 0.01, int? seed = null)
    {
        _mean = mean;
        _lsl = Math.Min(lsl, usl);
        _usl = Math.Max(lsl, usl);
        _sigma = Math.Max(1e-6, sigma);
        _kappa = kappa;
        _outlierProb = outlierProb;
        _rng = seed.HasValue ? new Random(seed.Value) : new Random();
        _x = Math.Clamp(NextGaussian(_mean, _sigma), _lsl, _usl);
    }

    public double Next()
    {
        double noise = NextGaussian(0.0, _sigma);
        _x = _x + _kappa * (_mean - _x) + noise;
        if (_rng.NextDouble() < _outlierProb)
        {
            double sign = _rng.NextDouble() < 0.5 ? -1.0 : 1.0;
            _x += sign * NextGaussian(2.8 * _sigma, 0.5 * _sigma);
        }
        return _x = Reflect(_x, _lsl, _usl);
    }

    private double NextGaussian(double mean, double std)
    {
        double u1 = Math.Max(1e-9, 1.0 - _rng.NextDouble());
        double u2 = 1.0 - _rng.NextDouble();
        double z = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
        return mean + std * z;
    }

    private static double Reflect(double v, double lo, double hi)
    {
        if (hi <= lo) return lo;
        while (v < lo || v > hi)
        {
            if (v < lo) v = lo + (lo - v);
            if (v > hi) v = hi - (v - hi);
        }
        return Math.Clamp(v, lo, hi);
    }
}

public sealed class SequenceNode : ObservableObject
{
    private SequenceNodeState _state = SequenceNodeState.Idle;
    private int _hitCount;
    private double _lastDurationMs;
    private string _lastResult = "-";
    private bool _hasBreakpoint;
    private bool _forceErrorArmed;

    public string Id { get; init; } = "";
    public SequenceNodeKind Kind { get; init; }
    public string RequiredEvent { get; set; } = "";
    public List<string> NextIds { get; } = [];

    internal Func<SequenceContext, bool>? ActionHandler { get; set; }
    internal Func<SequenceContext, IReadOnlyList<string>, string>? DecisionHandler { get; set; }
    internal Action<SequenceContext>? EventAcceptedHandler { get; set; }

    public SequenceNodeState State { get => _state; set => SetProperty(ref _state, value); }
    public int HitCount { get => _hitCount; set => SetProperty(ref _hitCount, value); }
    public double LastDurationMs { get => _lastDurationMs; set => SetProperty(ref _lastDurationMs, value); }
    public string LastResult { get => _lastResult; set => SetProperty(ref _lastResult, value); }
    public bool HasBreakpoint { get => _hasBreakpoint; set => SetProperty(ref _hasBreakpoint, value); }
    public bool ForceErrorArmed { get => _forceErrorArmed; set => SetProperty(ref _forceErrorArmed, value); }
}

public sealed class SequenceContext
{
    public HisLoggerHub Logger { get; }
    public HashSet<string> PendingEvents { get; } = new(StringComparer.OrdinalIgnoreCase);
    public bool IsCycleStopRequested { get; set; }
    public Dictionary<string, object> StateBag { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Action<int, string>? RaiseAlarmCallback { get; set; }

    public SequenceContext(HisLoggerHub logger) => Logger = logger;
}

public sealed class SequenceBuilder
{
    private readonly Dictionary<string, SequenceNode> _nodes = new(StringComparer.Ordinal);
    private readonly List<string> _order = [];
    private Action<SequenceContext, string>? _onForceError;

    public string SequenceId { get; }

    public SequenceBuilder(string sequenceId)
    {
        SequenceId = sequenceId;
        EnsureNode("Init", SequenceNodeKind.Init);
    }

    public SequenceBuilder AddAction(string id, Func<SequenceContext, bool>? action = null)
    {
        var n = EnsureNode(id, SequenceNodeKind.Action);
        n.ActionHandler = action ?? (_ => true);
        return this;
    }

    public SequenceBuilder AddDecision(string id, Func<SequenceContext, IReadOnlyList<string>, string> decide)
    {
        var n = EnsureNode(id, SequenceNodeKind.Decision);
        n.DecisionHandler = decide;
        return this;
    }

    public SequenceBuilder AddEventAcceptor(string id, string eventName, Action<SequenceContext>? onAccepted = null)
    {
        var n = EnsureNode(id, SequenceNodeKind.EventAcceptor);
        n.RequiredEvent = eventName;
        n.EventAcceptedHandler = onAccepted;
        return this;
    }

    public SequenceBuilder Flow(params string[] nodeChain)
    {
        for (int i = 0; i < nodeChain.Length - 1; i++)
        {
            var src = EnsureNode(nodeChain[i], InferKind(nodeChain[i]));
            EnsureNode(nodeChain[i + 1], InferKind(nodeChain[i + 1]));
            if (!src.NextIds.Contains(nodeChain[i + 1], StringComparer.Ordinal))
                src.NextIds.Add(nodeChain[i + 1]);
        }
        return this;
    }

    public SequenceBuilder OnForceError(Action<SequenceContext, string> handler)
    {
        _onForceError = handler;
        return this;
    }

    public SequenceRunner Build(SequenceContext context)
    {
        EnsureNode("Final", SequenceNodeKind.Final);
        var ordered = _order.Select(id => _nodes[id]).ToList();
        return new SequenceRunner(SequenceId, ordered, context, _onForceError);
    }

    private SequenceNode EnsureNode(string id, SequenceNodeKind kind)
    {
        if (!_nodes.TryGetValue(id, out var node))
        {
            node = new SequenceNode { Id = id, Kind = kind };
            _nodes[id] = node;
            _order.Add(id);
        }
        return node;
    }

    private static SequenceNodeKind InferKind(string id)
        => id == "Init" ? SequenceNodeKind.Init
         : id == "Final" ? SequenceNodeKind.Final
         : SequenceNodeKind.Action;
}

public sealed class SequenceRunner : ObservableObject
{
    private readonly SequenceContext _context;
    private readonly Action<SequenceContext, string>? _onForceError;
    private string _currentNodeId = "Init";
    private bool _isFinished;
    private bool _isHaltedOnError;
    private bool _isPausedAtBreakpoint;
    private bool _isEnabled = true;

    public string SequenceId { get; }
    public ObservableCollection<SequenceNode> Nodes { get; } = [];
    public SequenceContext Context => _context;

    public SequenceRunner(string sequenceId, IEnumerable<SequenceNode> nodes, SequenceContext context, Action<SequenceContext, string>? onForceError)
    {
        SequenceId = sequenceId;
        _context = context;
        _onForceError = onForceError;
        foreach (var n in nodes) Nodes.Add(n);
        var init = Nodes.FirstOrDefault(n => n.Id == "Init");
        if (init is not null) init.State = SequenceNodeState.Running;
    }

    public string CurrentNodeId { get => _currentNodeId; private set => SetProperty(ref _currentNodeId, value); }
    public bool IsFinished { get => _isFinished; private set => SetProperty(ref _isFinished, value); }
    public bool IsHaltedOnError { get => _isHaltedOnError; private set => SetProperty(ref _isHaltedOnError, value); }
    public bool IsPausedAtBreakpoint { get => _isPausedAtBreakpoint; private set => SetProperty(ref _isPausedAtBreakpoint, value); }
    public bool IsEnabled { get => _isEnabled; set => SetProperty(ref _isEnabled, value); }

    public void Reset()
    {
        foreach (var n in Nodes)
        {
            n.State = SequenceNodeState.Idle;
            n.ForceErrorArmed = false;
            n.LastResult = "-";
        }
        IsFinished = false;
        IsHaltedOnError = false;
        IsPausedAtBreakpoint = false;
        _context.IsCycleStopRequested = false;
        CurrentNodeId = "Init";
        var init = Nodes.FirstOrDefault(n => n.Id == "Init");
        if (init is not null) init.State = SequenceNodeState.Running;
        _context.Logger.Info(SequenceId, "Sequence reset to Init.");
    }

    public void ClearBreakpointPause() => IsPausedAtBreakpoint = false;

    public bool StepOneNode()
    {
        if (!IsEnabled || IsFinished || IsHaltedOnError) return false;

        var current = Nodes.FirstOrDefault(n => n.Id == CurrentNodeId);
        if (current is null) return false;

        if (current.HasBreakpoint && !IsPausedAtBreakpoint && current.State != SequenceNodeState.PausedAtBreakpoint)
        {
            current.State = SequenceNodeState.PausedAtBreakpoint;
            IsPausedAtBreakpoint = true;
            _context.Logger.Warn(SequenceId, $"Paused at breakpoint on node [{current.Id}]. Click Single Step or Resume.");
            return false;
        }
        IsPausedAtBreakpoint = false;

        if (current.ForceErrorArmed)
        {
            current.ForceErrorArmed = false;
            current.State = SequenceNodeState.Error;
            current.LastResult = "FORCE_ERROR";
            IsHaltedOnError = true;
            _context.Logger.Error(SequenceId, $"Force Error triggered at node [{current.Id}]!");
            _onForceError?.Invoke(_context, current.Id);
            _context.RaiseAlarmCallback?.Invoke(2001, $"[{SequenceId}] Force Error at {current.Id}");
            return false;
        }

        var sw = Stopwatch.StartNew();
        string? nextId = current.NextIds.FirstOrDefault();

        switch (current.Kind)
        {
            case SequenceNodeKind.Init:
                current.LastResult = "START";
                break;

            case SequenceNodeKind.Action:
                bool ok = current.ActionHandler?.Invoke(_context) ?? true;
                if (!ok)
                {
                    current.State = SequenceNodeState.Error;
                    current.LastResult = "NG/ALARM";
                    IsHaltedOnError = true;
                    _context.Logger.Error(SequenceId, $"Action [{current.Id}] returned NG.");
                    return false;
                }
                current.LastResult = "OK";
                break;

            case SequenceNodeKind.Decision:
                if (current.DecisionHandler is not null)
                    nextId = current.DecisionHandler(_context, current.NextIds);
                current.LastResult = $"-> {nextId}";
                break;

            case SequenceNodeKind.EventAcceptor:
                if (!_context.PendingEvents.Remove(current.RequiredEvent))
                {
                    current.State = SequenceNodeState.WaitingEvent;
                    current.LastResult = $"WAIT({current.RequiredEvent})";
                    return false;
                }
                current.EventAcceptedHandler?.Invoke(_context);
                current.LastResult = $"EVENT({current.RequiredEvent})";
                break;

            case SequenceNodeKind.Final:
                current.State = SequenceNodeState.Completed;
                current.HitCount++;
                current.LastResult = "FINAL";
                IsFinished = true;
                _context.Logger.Info(SequenceId, "Reached Final node (Cycle Stopped).");
                return false;
        }

        sw.Stop();
        current.HitCount++;
        current.LastDurationMs = Math.Round(Math.Max(1.2, sw.Elapsed.TotalMilliseconds), 2);
        current.State = SequenceNodeState.Completed;

        if (string.IsNullOrEmpty(nextId))
        {
            IsFinished = true;
            return false;
        }

        var nextNode = Nodes.FirstOrDefault(n => n.Id == nextId);
        if (nextNode is null)
        {
            IsFinished = true;
            return false;
        }

        CurrentNodeId = nextNode.Id;
        nextNode.State = nextNode.Kind == SequenceNodeKind.Final ? SequenceNodeState.Completed : SequenceNodeState.Running;
        if (nextNode.Kind == SequenceNodeKind.Final)
        {
            nextNode.HitCount++;
            IsFinished = true;
        }

        _context.Logger.Info(SequenceId, $"{current.Id} ({current.LastResult}) -> {nextNode.Id}");
        return true;
    }
}
