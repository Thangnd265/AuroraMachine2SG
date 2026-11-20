using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using AuroraMachine2SG.Core.Sdk;

namespace AuroraMachine2SG.App.Controls;

/// <summary>
/// Interactive visual state-machine flowchart renderer for SequenceRunner.
/// Renders nodes in topological order with forward arrows, right-side loop-back arcs,
/// live execution colors, breakpoint/force-error badges, and click-to-select node support.
/// </summary>
public sealed class SequenceFlowchartView : FrameworkElement
{
    public static readonly DependencyProperty SequenceProperty =
        DependencyProperty.Register(nameof(Sequence), typeof(SequenceRunner), typeof(SequenceFlowchartView),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender, OnSequenceChanged));

    public static readonly DependencyProperty SelectedNodeProperty =
        DependencyProperty.Register(nameof(SelectedNode), typeof(SequenceNode), typeof(SequenceFlowchartView),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public SequenceRunner? Sequence
    {
        get => (SequenceRunner?)GetValue(SequenceProperty);
        set => SetValue(SequenceProperty, value);
    }

    public SequenceNode? SelectedNode
    {
        get => (SequenceNode?)GetValue(SelectedNodeProperty);
        set => SetValue(SelectedNodeProperty, value);
    }

    public event Action<SequenceNode>? NodeSelected;

    private readonly List<(SequenceNode Node, Rect Bounds)> _hitBoxes = [];

    private static void OnSequenceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var view = (SequenceFlowchartView)d;
        if (e.OldValue is SequenceRunner oldSeq)
        {
            oldSeq.PropertyChanged -= view.OnModelChanged;
            foreach (var n in oldSeq.Nodes) n.PropertyChanged -= view.OnModelChanged;
        }
        if (e.NewValue is SequenceRunner newSeq)
        {
            newSeq.PropertyChanged += view.OnModelChanged;
            foreach (var n in newSeq.Nodes) n.PropertyChanged += view.OnModelChanged;
        }
        view.InvalidateMeasure();
        view.InvalidateVisual();
    }

    private void OnModelChanged(object? sender, PropertyChangedEventArgs e)
        => Dispatcher.InvokeAsync(InvalidateVisual);

    protected override Size MeasureOverride(Size availableSize)
    {
        int count = Sequence?.Nodes.Count ?? 0;
        double h = Math.Max(420, 36 + count * 76);
        double w = double.IsInfinity(availableSize.Width) ? 680 : Math.Max(560, availableSize.Width);
        return new Size(w, h);
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        var pt = e.GetPosition(this);
        foreach (var (node, rect) in _hitBoxes)
        {
            if (rect.Contains(pt))
            {
                SelectedNode = node;
                NodeSelected?.Invoke(node);
                InvalidateVisual();
                break;
            }
        }
        base.OnMouseLeftButtonDown(e);
    }

    protected override void OnRender(DrawingContext dc)
    {
        _hitBoxes.Clear();
        dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(0xFA, 0xFC, 0xFF)), null, new Rect(0, 0, ActualWidth, ActualHeight));

        var seq = Sequence;
        if (seq is null || seq.Nodes.Count == 0) return;

        double nodeW = Math.Min(330, Math.Max(230, ActualWidth * 0.50));
        double nodeH = 50;
        double gapY = 24;
        double startX = Math.Max(28, (ActualWidth - nodeW) * 0.34);
        double startY = 20;

        var indexMap = new Dictionary<string, int>(StringComparer.Ordinal);
        var rectMap = new Dictionary<string, Rect>(StringComparer.Ordinal);

        for (int i = 0; i < seq.Nodes.Count; i++)
        {
            var n = seq.Nodes[i];
            indexMap[n.Id] = i;
            // Offset secondary branch nodes (e.g. EventAcceptor) slightly to the left/right if desired, or clean vertical spine
            double xOffset = n.Kind == SequenceNodeKind.EventAcceptor ? -22 : 0;
            var r = new Rect(startX + xOffset, startY + i * (nodeH + gapY), nodeW, nodeH);
            rectMap[n.Id] = r;
            _hitBoxes.Add((n, r));
        }

        var forwardPen = new Pen(new SolidColorBrush(Color.FromRgb(0x47, 0x55, 0x69)), 1.6);
        var loopPen = new Pen(new SolidColorBrush(Color.FromRgb(0x25, 0x63, 0xEB)), 1.6) { DashStyle = DashStyles.Dash };

        // 1. Draw Edges
        int loopLane = 0;
        foreach (var n in seq.Nodes)
        {
            var srcRect = rectMap[n.Id];
            int srcIdx = indexMap[n.Id];

            foreach (var dstId in n.NextIds)
            {
                if (!rectMap.TryGetValue(dstId, out var dstRect)) continue;
                int dstIdx = indexMap[dstId];

                if (dstIdx == srcIdx + 1)
                {
                    // Direct downward arrow
                    var p1 = new Point(srcRect.Left + srcRect.Width / 2, srcRect.Bottom);
                    var p2 = new Point(dstRect.Left + dstRect.Width / 2, dstRect.Top);
                    dc.DrawLine(forwardPen, p1, p2);
                    DrawArrowHead(dc, p2, 0, 1, forwardPen.Brush);
                }
                else if (dstIdx > srcIdx + 1)
                {
                    // Forward skip branch on left side
                    double lx = Math.Max(10, srcRect.Left - 24 - (loopLane % 3) * 12);
                    loopLane++;
                    var geom = new StreamGeometry();
                    using (var ctx = geom.Open())
                    {
                        ctx.BeginFigure(new Point(srcRect.Left, srcRect.Top + srcRect.Height / 2), false, false);
                        ctx.LineTo(new Point(lx, srcRect.Top + srcRect.Height / 2), true, false);
                        ctx.LineTo(new Point(lx, dstRect.Top + dstRect.Height / 2), true, false);
                        ctx.LineTo(new Point(dstRect.Left, dstRect.Top + dstRect.Height / 2), true, false);
                    }
                    dc.DrawGeometry(null, forwardPen, geom);
                    DrawArrowHead(dc, new Point(dstRect.Left, dstRect.Top + dstRect.Height / 2), 1, 0, forwardPen.Brush);
                }
                else
                {
                    // Loop-back arc on right side
                    double rx = Math.Min(ActualWidth - 14, srcRect.Right + 28 + (loopLane % 4) * 16);
                    loopLane++;
                    var geom = new StreamGeometry();
                    using (var ctx = geom.Open())
                    {
                        ctx.BeginFigure(new Point(srcRect.Right, srcRect.Top + srcRect.Height / 2), false, false);
                        ctx.LineTo(new Point(rx, srcRect.Top + srcRect.Height / 2), true, false);
                        ctx.LineTo(new Point(rx, dstRect.Top + dstRect.Height / 2), true, false);
                        ctx.LineTo(new Point(dstRect.Right, dstRect.Top + dstRect.Height / 2), true, false);
                    }
                    dc.DrawGeometry(null, loopPen, geom);
                    DrawArrowHead(dc, new Point(dstRect.Right, dstRect.Top + dstRect.Height / 2), -1, 0, loopPen.Brush);
                    PieRatioChartView.DrawText(dc, "LOOP", rx - 32, (srcRect.Top + dstRect.Top) / 2, 8.5, FontWeights.Bold, loopPen.Brush);
                }
            }
        }

        // 2. Draw Nodes
        foreach (var (node, rect) in _hitBoxes)
        {
            var (fill, border, badgeText) = GetNodeVisuals(node);
            bool isSelected = ReferenceEquals(node, SelectedNode);
            var pen = new Pen(isSelected ? new SolidColorBrush(Color.FromRgb(0x1D, 0x4E, 0xD8)) : border, isSelected ? 2.8 : 1.6);

            double radius = node.Kind is SequenceNodeKind.Init or SequenceNodeKind.Final ? 22 : 7;
            dc.DrawRoundedRectangle(fill, pen, rect, radius, radius);

            // Kind tag on top-left + Node ID
            string kindTag = node.Kind switch
            {
                SequenceNodeKind.Decision => "◆ DECIDE",
                SequenceNodeKind.EventAcceptor => "⚡ EVENT",
                SequenceNodeKind.Init => "▶ START",
                SequenceNodeKind.Final => "■ END",
                _ => "● ACTION"
            };
            PieRatioChartView.DrawText(dc, $"{kindTag}  {node.Id}", rect.Left + 12, rect.Top + 7, 11.5, FontWeights.Bold, Brushes.Black);

            // Bottom telemetry line inside node
            string meta = $"Hits: {node.HitCount}   |   {node.LastDurationMs:F1} ms   |   {node.LastResult}";
            PieRatioChartView.DrawText(dc, meta, rect.Left + 12, rect.Top + 28, 9.8, FontWeights.Medium, new SolidColorBrush(Color.FromRgb(0x33, 0x41, 0x55)));

            // State badge on top-right
            if (!string.IsNullOrEmpty(badgeText))
                PieRatioChartView.DrawText(dc, badgeText, rect.Right - 68, rect.Top + 7, 9.5, FontWeights.ExtraBold, border);

            // Breakpoint / ForceError indicators on left rim
            if (node.HasBreakpoint)
                dc.DrawEllipse(Brushes.Crimson, new Pen(Brushes.White, 1), new Point(rect.Left + 2, rect.Top + 12), 6, 6);
            if (node.ForceErrorArmed)
                dc.DrawEllipse(Brushes.DarkOrange, new Pen(Brushes.Black, 1), new Point(rect.Left + 2, rect.Bottom - 12), 6, 6);
        }
    }

    private static (Brush Fill, Brush Border, string Badge) GetNodeVisuals(SequenceNode n) => n.State switch
    {
        SequenceNodeState.Running => (new SolidColorBrush(Color.FromRgb(0xBB, 0xF7, 0xD0)), new SolidColorBrush(Color.FromRgb(0x15, 0x80, 0x3D)), "RUNNING"),
        SequenceNodeState.Completed => (new SolidColorBrush(Color.FromRgb(0xE0, 0xF2, 0xFE)), new SolidColorBrush(Color.FromRgb(0x02, 0x84, 0xC7)), "DONE"),
        SequenceNodeState.WaitingEvent => (new SolidColorBrush(Color.FromRgb(0xFE, 0xF0, 0x8A)), new SolidColorBrush(Color.FromRgb(0xCA, 0x8A, 0x04)), "WAITING"),
        SequenceNodeState.PausedAtBreakpoint => (new SolidColorBrush(Color.FromRgb(0xED, 0xE9, 0xFE)), new SolidColorBrush(Color.FromRgb(0x7C, 0x3A, 0xED)), "BREAK"),
        SequenceNodeState.Error => (new SolidColorBrush(Color.FromRgb(0xFE, 0xCA, 0xCA)), new SolidColorBrush(Color.FromRgb(0xDC, 0x26, 0x26)), "ERROR!"),
        _ => (Brushes.White, new SolidColorBrush(Color.FromRgb(0x94, 0xA3, 0xB8)), "")
    };

    private static void DrawArrowHead(DrawingContext dc, Point tip, double dx, double dy, Brush brush)
    {
        double size = 5.5;
        Point p1, p2;
        if (Math.Abs(dy) > 0.5)
        {
            p1 = new Point(tip.X - size, tip.Y - dy * size * 1.4);
            p2 = new Point(tip.X + size, tip.Y - dy * size * 1.4);
        }
        else
        {
            p1 = new Point(tip.X - dx * size * 1.4, tip.Y - size);
            p2 = new Point(tip.X - dx * size * 1.4, tip.Y + size);
        }
        var g = new StreamGeometry();
        using (var ctx = g.Open())
        {
            ctx.BeginFigure(tip, true, true);
            ctx.LineTo(p1, true, false);
            ctx.LineTo(p2, true, false);
        }
        dc.DrawGeometry(brush, null, g);
    }
}
