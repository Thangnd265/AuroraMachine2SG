using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AuroraMachine2SG.Core.Models;
using AuroraMachine2SG.Core.Services;

namespace AuroraMachine2SG.App.Controls;

/// <summary>
/// Matches AuroraUIComponent.UserControls.BorderMonitor (green normal / red alarm).
/// </summary>
public sealed class BorderMonitor : ContentControl
{
    public static readonly DependencyProperty IsNormalProperty =
        DependencyProperty.Register(nameof(IsNormal), typeof(bool), typeof(BorderMonitor),
            new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));

    public bool IsNormal
    {
        get => (bool)GetValue(IsNormalProperty);
        set => SetValue(IsNormalProperty, value);
    }

    protected override void OnRender(DrawingContext dc)
    {
        var rect = new Rect(0.5, 0.5, Math.Max(0, ActualWidth - 1), Math.Max(0, ActualHeight - 1));
        var bg = IsNormal ? new SolidColorBrush(Color.FromRgb(0xEA, 0xF7, 0xE6)) : new SolidColorBrush(Color.FromRgb(0xFD, 0xE2, 0xE2));
        var border = IsNormal ? new Pen(new SolidColorBrush(Color.FromRgb(0x68, 0xB8, 0x5B)), 1.5) : new Pen(new SolidColorBrush(Color.FromRgb(0xC8, 0x25, 0x06)), 2.0);
        dc.DrawRoundedRectangle(bg, border, rect, 5, 5);
        base.OnRender(dc);
    }
}

/// <summary>
/// Crisp pixel-aligned 21x21 QR Code renderer for UCMaintCounter and HIVE popups.
/// </summary>
public sealed class QrCodeView : FrameworkElement
{
    public static readonly DependencyProperty PayloadProperty =
        DependencyProperty.Register(nameof(Payload), typeof(string), typeof(QrCodeView),
            new FrameworkPropertyMetadata("HIVE|RSMS-001", FrameworkPropertyMetadataOptions.AffectsRender));

    public string Payload
    {
        get => (string)GetValue(PayloadProperty);
        set => SetValue(PayloadProperty, value);
    }

    protected override void OnRender(DrawingContext dc)
    {
        double size = Math.Min(ActualWidth, ActualHeight);
        if (size < 10) return;

        dc.DrawRectangle(Brushes.White, new Pen(Brushes.LightGray, 0.8), new Rect(0, 0, ActualWidth, ActualHeight));
        var matrix = QrMatrixGenerator.Generate(Payload ?? "HIVE");
        int n = matrix.GetLength(0);
        double pad = 3.0;
        double cell = (size - pad * 2) / n;
        double ox = (ActualWidth - size) / 2.0 + pad;
        double oy = (ActualHeight - size) / 2.0 + pad;

        for (int r = 0; r < n; r++)
            for (int c = 0; c < n; c++)
                if (matrix[r, c])
                    dc.DrawRectangle(Brushes.Black, null, new Rect(ox + c * cell, oy + r * cell, cell + 0.35, cell + 0.35));
    }
}

/// <summary>
/// Matches UCHomePieChartView (PASS vs NG yield donut chart).
/// </summary>
public sealed class PieRatioChartView : FrameworkElement
{
    public static readonly DependencyProperty PassCountProperty =
        DependencyProperty.Register(nameof(PassCount), typeof(int), typeof(PieRatioChartView),
            new FrameworkPropertyMetadata(100, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty NgCountProperty =
        DependencyProperty.Register(nameof(NgCount), typeof(int), typeof(PieRatioChartView),
            new FrameworkPropertyMetadata(2, FrameworkPropertyMetadataOptions.AffectsRender));

    public int PassCount { get => (int)GetValue(PassCountProperty); set => SetValue(PassCountProperty, value); }
    public int NgCount { get => (int)GetValue(NgCountProperty); set => SetValue(NgCountProperty, value); }

    protected override void OnRender(DrawingContext dc)
    {
        if (ActualWidth < 40 || ActualHeight < 40) return;
        int total = Math.Max(1, PassCount + NgCount);
        double passRatio = (double)PassCount / total;

        double radius = Math.Min(ActualWidth * 0.34, ActualHeight * 0.42);
        var center = new Point(ActualWidth * 0.36, ActualHeight * 0.52);

        var green = new SolidColorBrush(Color.FromRgb(0x63, 0xC1, 0x54));
        var red = new SolidColorBrush(Color.FromRgb(0xE1, 0x53, 0x58));

        if (passRatio >= 0.999)
        {
            dc.DrawEllipse(green, null, center, radius, radius);
        }
        else
        {
            DrawSlice(dc, center, radius, -90, -90 + passRatio * 360.0, green);
            DrawSlice(dc, center, radius, -90 + passRatio * 360.0, 270, red);
        }

        dc.DrawEllipse(Brushes.GhostWhite, null, center, radius * 0.54, radius * 0.54);
        DrawText(dc, $"{passRatio * 100:F1}%", center.X - 24, center.Y - 9, 13, FontWeights.Bold, Brushes.Black);

        // Legend on right
        double lx = ActualWidth * 0.68;
        DrawText(dc, "Yield Ratio", lx - 6, 8, 11, FontWeights.Bold, Brushes.DimGray);
        dc.DrawRectangle(green, null, new Rect(lx, ActualHeight * 0.36, 12, 12));
        DrawText(dc, $"OK : {PassCount}", lx + 17, ActualHeight * 0.34, 12, FontWeights.SemiBold, Brushes.Black);
        dc.DrawRectangle(red, null, new Rect(lx, ActualHeight * 0.58, 12, 12));
        DrawText(dc, $"NG : {NgCount}", lx + 17, ActualHeight * 0.56, 12, FontWeights.SemiBold, Brushes.Black);
    }

    private static void DrawSlice(DrawingContext dc, Point c, double r, double startDeg, double endDeg, Brush fill)
    {
        double sRad = startDeg * Math.PI / 180.0;
        double eRad = endDeg * Math.PI / 180.0;
        var p1 = new Point(c.X + r * Math.Cos(sRad), c.Y + r * Math.Sin(sRad));
        var p2 = new Point(c.X + r * Math.Cos(eRad), c.Y + r * Math.Sin(eRad));
        bool isLarge = (endDeg - startDeg) > 180.0;

        var geom = new StreamGeometry();
        using (var ctx = geom.Open())
        {
            ctx.BeginFigure(c, true, true);
            ctx.LineTo(p1, true, false);
            ctx.ArcTo(p2, new Size(r, r), 0, isLarge, SweepDirection.Clockwise, true, false);
        }
        dc.DrawGeometry(fill, null, geom);
    }

    internal static void DrawText(DrawingContext dc, string text, double x, double y, double size, FontWeight weight, Brush brush)
    {
        var ft = new FormattedText(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, weight, FontStretches.Normal),
            size, brush, 1.0);
        dc.DrawText(ft, new Point(x, y));
    }
}

/// <summary>
/// Matches UCHomeScrewView (Angle vs Torque Nm curve with red dashed NG limit).
/// </summary>
public sealed class TorqueCurveView : FrameworkElement
{
    public static readonly DependencyProperty CurveProperty =
        DependencyProperty.Register(nameof(Curve), typeof(IReadOnlyList<(double Angle, double Torque)>), typeof(TorqueCurveView),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty NgLimitProperty =
        DependencyProperty.Register(nameof(NgLimit), typeof(double), typeof(TorqueCurveView),
            new FrameworkPropertyMetadata(3.8, FrameworkPropertyMetadataOptions.AffectsRender));

    public IReadOnlyList<(double Angle, double Torque)>? Curve
    {
        get => (IReadOnlyList<(double Angle, double Torque)>?)GetValue(CurveProperty);
        set => SetValue(CurveProperty, value);
    }

    public double NgLimit { get => (double)GetValue(NgLimitProperty); set => SetValue(NgLimitProperty, value); }

    protected override void OnRender(DrawingContext dc)
    {
        if (ActualWidth < 60 || ActualHeight < 50) return;
        var plot = new Rect(34, 10, ActualWidth - 44, ActualHeight - 32);
        dc.DrawRectangle(Brushes.White, new Pen(Brushes.Silver, 1), plot);

        var gridPen = new Pen(new SolidColorBrush(Color.FromRgb(0xEC, 0xF0, 0xF4)), 1);
        for (int y = 0; y <= 5; y++)
        {
            double py = plot.Bottom - (y / 5.0) * plot.Height;
            dc.DrawLine(gridPen, new Point(plot.Left, py), new Point(plot.Right, py));
            PieRatioChartView.DrawText(dc, y.ToString(), 14, py - 7, 9.5, FontWeights.Normal, Brushes.DimGray);
        }

        // Red dashed NG limit line
        double ngY = plot.Bottom - (Math.Clamp(NgLimit, 0, 5.0) / 5.0) * plot.Height;
        var dashPen = new Pen(Brushes.Red, 1.6) { DashStyle = DashStyles.Dash };
        dc.DrawLine(dashPen, new Point(plot.Left, ngY), new Point(plot.Right, ngY));
        PieRatioChartView.DrawText(dc, $"NG {NgLimit:F1}Nm", plot.Right - 60, ngY - 14, 9.5, FontWeights.Bold, Brushes.Red);

        var pts = Curve;
        if (pts is { Count: > 1 })
        {
            double maxAngle = Math.Max(3600.0, pts.Max(p => p.Angle));
            var geom = new StreamGeometry();
            using (var ctx = geom.Open())
            {
                for (int i = 0; i < pts.Count; i++)
                {
                    double px = plot.Left + (pts[i].Angle / maxAngle) * plot.Width;
                    double py = plot.Bottom - (Math.Clamp(pts[i].Torque, 0, 5.0) / 5.0) * plot.Height;
                    if (i == 0) ctx.BeginFigure(new Point(px, py), false, false);
                    else ctx.LineTo(new Point(px, py), true, true);
                }
            }
            dc.DrawGeometry(null, new Pen(new SolidColorBrush(Color.FromRgb(0x1D, 0x4E, 0xD8)), 2.2), geom);
        }

        PieRatioChartView.DrawText(dc, "Angle (0 - 3600 deg)", plot.Left + plot.Width * 0.30, plot.Bottom + 4, 9.5, FontWeights.SemiBold, Brushes.DimGray);
    }
}

/// <summary>
/// Matches UCHomeDailyChartView & UCHomeWeeklyChartView column bar chart.
/// </summary>
public sealed class BarTrendChartView : FrameworkElement
{
    public static readonly DependencyProperty ValuesProperty =
        DependencyProperty.Register(nameof(Values), typeof(IReadOnlyList<double>), typeof(BarTrendChartView),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty IsWeeklyProperty =
        DependencyProperty.Register(nameof(IsWeekly), typeof(bool), typeof(BarTrendChartView),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

    public IReadOnlyList<double>? Values { get => (IReadOnlyList<double>?)GetValue(ValuesProperty); set => SetValue(ValuesProperty, value); }
    public bool IsWeekly { get => (bool)GetValue(IsWeeklyProperty); set => SetValue(IsWeeklyProperty, value); }

    private static readonly string[] Days = ["Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"];

    protected override void OnRender(DrawingContext dc)
    {
        if (ActualWidth < 60 || ActualHeight < 45) return;
        var plot = new Rect(28, 8, ActualWidth - 36, ActualHeight - 28);
        dc.DrawRectangle(Brushes.White, new Pen(Brushes.Gainsboro, 1), plot);

        var vals = Values;
        if (vals is null || vals.Count == 0) return;

        double max = Math.Max(10, vals.Max() * 1.15);
        double slotW = plot.Width / vals.Count;
        var barBrush = IsWeekly
            ? new SolidColorBrush(Color.FromRgb(0x4F, 0x46, 0xE5))
            : new SolidColorBrush(Color.FromRgb(0x3B, 0x82, 0xF6));

        for (int i = 0; i < vals.Count; i++)
        {
            double bh = (vals[i] / max) * (plot.Height - 6);
            double bx = plot.Left + i * slotW + slotW * 0.18;
            double bw = Math.Max(4, slotW * 0.64);
            dc.DrawRoundedRectangle(barBrush, null, new Rect(bx, plot.Bottom - bh, bw, bh), 2, 2);

            if (IsWeekly && i < Days.Length)
                PieRatioChartView.DrawText(dc, Days[i], bx, plot.Bottom + 3, 9, FontWeights.Normal, Brushes.DimGray);
            else if (!IsWeekly && i % 4 == 0)
                PieRatioChartView.DrawText(dc, $"{i:00}", bx - 2, plot.Bottom + 3, 8.5, FontWeights.Normal, Brushes.DimGray);
        }
    }
}

/// <summary>
/// Matches SGComponent.CPK.UCCpkChart (PDCA trend + LSL/USL constant lines).
/// </summary>
public sealed class CpkChartView : FrameworkElement
{
    public static readonly DependencyProperty ItemProperty =
        DependencyProperty.Register(nameof(Item), typeof(PdcaMeasurementItem), typeof(CpkChartView),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public PdcaMeasurementItem? Item { get => (PdcaMeasurementItem?)GetValue(ItemProperty); set => SetValue(ItemProperty, value); }

    protected override void OnRender(DrawingContext dc)
    {
        if (ActualWidth < 80 || ActualHeight < 60) return;
        var plot = new Rect(40, 8, ActualWidth - 52, ActualHeight - 24);
        dc.DrawRectangle(Brushes.White, new Pen(Brushes.LightSlateGray, 1), plot);

        var item = Item;
        if (item is null || item.DataPoints.Count == 0) return;

        double margin = (item.USL - item.LSL) * 0.25;
        double minY = Math.Min(item.LSL - margin, item.DataPoints.Min(p => p.Value));
        double maxY = Math.Max(item.USL + margin, item.DataPoints.Max(p => p.Value));
        if (Math.Abs(maxY - minY) < 1e-9) maxY = minY + 1.0;

        double MapY(double v) => plot.Bottom - ((v - minY) / (maxY - minY)) * plot.Height;

        var specPen = new Pen(Brushes.Red, 1.4) { DashStyle = DashStyles.Dash };
        double yUsl = MapY(item.USL);
        double yLsl = MapY(item.LSL);
        dc.DrawLine(specPen, new Point(plot.Left, yUsl), new Point(plot.Right, yUsl));
        dc.DrawLine(specPen, new Point(plot.Left, yLsl), new Point(plot.Right, yLsl));
        PieRatioChartView.DrawText(dc, $"USL {item.USL:F3}", plot.Left + 4, yUsl - 13, 9, FontWeights.Bold, Brushes.Red);
        PieRatioChartView.DrawText(dc, $"LSL {item.LSL:F3}", plot.Left + 4, yLsl + 2, 9, FontWeights.Bold, Brushes.Red);

        double mean = item.DataPoints.Average(p => p.Value);
        double yMean = MapY(mean);
        dc.DrawLine(new Pen(Brushes.SeaGreen, 1.2), new Point(plot.Left, yMean), new Point(plot.Right, yMean));

        var geom = new StreamGeometry();
        int cnt = item.DataPoints.Count;
        var dotBrush = new SolidColorBrush(Color.FromRgb(0x1D, 0x4E, 0xD8));
        using (var ctx = geom.Open())
        {
            for (int i = 0; i < cnt; i++)
            {
                double px = plot.Left + (cnt == 1 ? 0.5 : (double)i / (cnt - 1)) * plot.Width;
                double py = MapY(item.DataPoints[i].Value);
                if (i == 0) ctx.BeginFigure(new Point(px, py), false, false);
                else ctx.LineTo(new Point(px, py), true, true);
                dc.DrawEllipse(dotBrush, null, new Point(px, py), 2.5, 2.5);
            }
        }
        dc.DrawGeometry(null, new Pen(dotBrush, 1.5), geom);
    }
}
