using AuroraMachine2SG.Core.Models;

namespace AuroraMachine2SG.Core.Services;

public sealed class SpcResult(
    int n, double mean, double stdDev, double min, double max,
    double cp, double cpu, double cpl, double cpk,
    DateTime startTime, DateTime endTime, double uph)
{
    public int N { get; } = n;
    public double Mean { get; } = mean;
    public double StdDev { get; } = stdDev;
    public double Min { get; } = min;
    public double Max { get; } = max;
    public double Cp { get; } = cp;
    public double Cpu { get; } = cpu;
    public double Cpl { get; } = cpl;
    public double Cpk { get; } = cpk;
    public DateTime StartTime { get; } = startTime;
    public DateTime EndTime { get; } = endTime;
    public TimeSpan Elapsed => EndTime - StartTime;
    public double UPH { get; } = uph;
}

public static class SpcCalculator
{
    public static SpcResult Calculate(PdcaMeasurementItem item, bool useSampleStd = true)
    {
        ArgumentNullException.ThrowIfNull(item);

        var validPoints = (item.DataPoints ?? [])
            .Where(p => p is not null && !double.IsNaN(p.Value) && !double.IsInfinity(p.Value))
            .ToArray();

        int n = validPoints.Length;
        if (n == 0)
            return new SpcResult(0, 0, 0, 0, 0, 0, 0, 0, 0, item.StartTime, item.EndTime, 0);

        var values = validPoints.Select(p => p.Value).ToArray();
        double mean = values.Average();
        double min = values.Min();
        double max = values.Max();

        double std = double.NaN;
        if (useSampleStd)
        {
            if (n >= 2)
                std = Math.Sqrt(values.Sum(v => (v - mean) * (v - mean)) / (n - 1));
        }
        else
        {
            std = Math.Sqrt(values.Sum(v => (v - mean) * (v - mean)) / n);
        }

        double cp = 0, cpu = 0, cpl = 0, cpk = 0;
        if (item.USL > item.LSL && std > 0.0 && !double.IsNaN(std))
        {
            cp = (item.USL - item.LSL) / (6.0 * std);
            cpu = (item.USL - mean) / (3.0 * std);
            cpl = (mean - item.LSL) / (3.0 * std);
            cpk = Math.Min(cpu, cpl);
        }

        DateTime start = item.StartTime == default ? validPoints.Min(p => p.TimeStamp) : item.StartTime;
        DateTime end = item.EndTime == default ? validPoints.Max(p => p.TimeStamp) : item.EndTime;
        if (end < start) (start, end) = (end, start);

        double hours = Math.Max((end - start).TotalHours, 0.0);
        double uph = hours > 0.0 ? n / hours : 0.0;

        return new SpcResult(n, mean, std, min, max, cp, cpu, cpl, cpk, start, end, uph);
    }
}
