using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using AuroraMachine2SG.Core.Models;

namespace AuroraMachine2SG.App.Themes;

public sealed class BoolToVisibilityExConverter : IValueConverter
{
    public bool Invert { get; set; }
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool flag = value is bool b && b;
        if (Invert) flag = !flag;
        return flag ? Visibility.Visible : Visibility.Collapsed;
    }
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is Visibility.Visible;
}

public sealed class InverseBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => !(value is bool b && b);
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => !(value is bool b && b);
}

public sealed class StringEqualsToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => string.Equals(value?.ToString(), parameter?.ToString(), StringComparison.OrdinalIgnoreCase)
            ? Visibility.Visible
            : Visibility.Collapsed;
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}

public sealed class StringEqualsToBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => string.Equals(value?.ToString(), parameter?.ToString(), StringComparison.OrdinalIgnoreCase);
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true && parameter is not null ? Enum.Parse(targetType, parameter.ToString()!) : Binding.DoNothing;
}

public sealed class HiveStateToBrushConverter : IValueConverter
{
    private static SolidColorBrush Hex(string h) => (SolidColorBrush)new BrushConverter().ConvertFromString(h)!;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is HiveState st ? st switch
        {
            HiveState.Running => Hex("#8DD471"),
            HiveState.Idle => Hex("#518FFF"),
            HiveState.Engineering => Hex("#4233A6"),
            HiveState.PlannedDT => Hex("#FFDA5A"),
            HiveState.UnplannedDT => Hex("#E15358"),
            _ => Hex("#D6D6D6")
        } : Hex("#D6D6D6");

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => Binding.DoNothing;
}

public sealed class UsedSlotToBrushConverter : IValueConverter
{
    private static readonly SolidColorBrush UsedBrush = new(Color.FromRgb(0x9E, 0x9E, 0x9E));
    private static readonly SolidColorBrush FreeBrush = new(Color.FromRgb(0xA3, 0xDA, 0x97));
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? UsedBrush : FreeBrush;
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => Binding.DoNothing;
}

public sealed class ProductPresentToBrushConverter : IValueConverter
{
    private static readonly SolidColorBrush ActiveBrush = new(Color.FromRgb(0xFD, 0xFD, 0xBF));
    private static readonly SolidColorBrush EmptyBrush = new(Color.FromRgb(0xFA, 0xFA, 0xFC));
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? ActiveBrush : EmptyBrush;
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => Binding.DoNothing;
}

public sealed class ActivePillBrushConverter : IValueConverter
{
    private static readonly SolidColorBrush OnBrush = new(Color.FromRgb(0x8D, 0xD4, 0x71));
    private static readonly SolidColorBrush OffBrush = new(Color.FromRgb(0xC8, 0x25, 0x06));
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? OnBrush : OffBrush;
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => Binding.DoNothing;
}

public sealed class JudgeToBrushConverter : IValueConverter
{
    private static readonly SolidColorBrush PassBrush = new(Color.FromRgb(0x1E, 0x8E, 0x3E));
    private static readonly SolidColorBrush NgBrush = new(Color.FromRgb(0xC8, 0x25, 0x06));
    private static readonly SolidColorBrush NeutralBrush = new(Color.FromRgb(0x5F, 0x63, 0x68));

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => (value?.ToString()?.ToUpperInvariant()) switch
        {
            "PASS" => PassBrush,
            "NG" or "ALARM" => NgBrush,
            _ => NeutralBrush
        };
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => Binding.DoNothing;
}
