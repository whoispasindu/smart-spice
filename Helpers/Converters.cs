using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using SmartSpice.Models;

namespace SmartSpice.Helpers;

/// <summary>Visible when the value is "truthy": true, or a non-null object.</summary>
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c)
    {
        bool truthy = value is bool b ? b : value != null;
        return truthy ? Visibility.Visible : Visibility.Collapsed;
    }
    public object ConvertBack(object value, Type t, object p, CultureInfo c)
        => value is Visibility v && v == Visibility.Visible;
}

/// <summary>Multiplies a 0..1 fraction by the parameter (max width) for bar charts.</summary>
public class FractionToWidthConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c)
    {
        double frac = value is double d ? d : 0;
        double max = 240;
        if (p != null && double.TryParse(p.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var m))
            max = m;
        return Math.Max(0, Math.Min(1, frac)) * max;
    }
    public object ConvertBack(object value, Type t, object p, CultureInfo c)
        => throw new NotSupportedException();
}

/// <summary>true → green, false → red. Used for low-stock / pass-fail flags.</summary>
public class BoolToStatusBrushConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c)
    {
        bool good = value is bool b && b;
        // parameter "invert" flips the meaning (e.g. IsLowStock true == bad).
        if (p?.ToString() == "invert") good = !good;
        return good ? new SolidColorBrush(Color.FromRgb(0x2E, 0x7D, 0x32))
                    : new SolidColorBrush(Color.FromRgb(0xC6, 0x28, 0x28));
    }
    public object ConvertBack(object value, Type t, object p, CultureInfo c)
        => throw new NotSupportedException();
}

/// <summary>Maps a quality grade to a representative colour.</summary>
public class GradeToBrushConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c)
    {
        var color = value is QualityGrade g ? g switch
        {
            QualityGrade.A => Color.FromRgb(0x2E, 0x7D, 0x32),
            QualityGrade.B => Color.FromRgb(0x7C, 0xB3, 0x42),
            QualityGrade.C => Color.FromRgb(0xF9, 0xA8, 0x25),
            QualityGrade.D => Color.FromRgb(0xEF, 0x6C, 0x00),
            _ => Color.FromRgb(0xC6, 0x28, 0x28),
        } : Color.FromRgb(0x9E, 0x9E, 0x9E);
        return new SolidColorBrush(color);
    }
    public object ConvertBack(object value, Type t, object p, CultureInfo c)
        => throw new NotSupportedException();
}

/// <summary>Maps notification severity to a colour.</summary>
public class SeverityToBrushConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c)
    {
        var color = value is NotificationSeverity s ? s switch
        {
            NotificationSeverity.Critical => Color.FromRgb(0xC6, 0x28, 0x28),
            NotificationSeverity.Warning => Color.FromRgb(0xEF, 0x6C, 0x00),
            _ => Color.FromRgb(0x15, 0x65, 0xC0),
        } : Color.FromRgb(0x15, 0x65, 0xC0);
        return new SolidColorBrush(color);
    }
    public object ConvertBack(object value, Type t, object p, CultureInfo c)
        => throw new NotSupportedException();
}

/// <summary>A numeric percent → a proportional star GridLength (for fill bars).</summary>
public class PercentToStarConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c)
    {
        double v = value is double d ? d : 0;
        return new GridLength(Math.Max(0, v), GridUnitType.Star);
    }
    public object ConvertBack(object value, Type t, object p, CultureInfo c)
        => throw new NotSupportedException();
}

/// <summary>Fill level → colour: green &lt;75%, amber &lt;90%, red otherwise.</summary>
public class PercentToLevelBrushConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c)
    {
        double v = value is double d ? d : 0;
        Color col = v >= 90 ? Color.FromRgb(0xD1, 0x43, 0x43)
                  : v >= 75 ? Color.FromRgb(0xEF, 0x8A, 0x1F)
                  : Color.FromRgb(0x3E, 0x8E, 0x2E);
        return new SolidColorBrush(col);
    }
    public object ConvertBack(object value, Type t, object p, CultureInfo c)
        => throw new NotSupportedException();
}

/// <summary>value == parameter (case-insensitive) → true (for nav highlight binding).</summary>
public class StringEqualsToBoolConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c)
        => string.Equals(value?.ToString(), p?.ToString(), StringComparison.OrdinalIgnoreCase);
    public object ConvertBack(object value, Type t, object p, CultureInfo c)
        => throw new NotSupportedException();
}

/// <summary>value == parameter (case-insensitive) → Visible, else Collapsed.</summary>
public class StringEqualsToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c)
        => string.Equals(value?.ToString(), p?.ToString(), StringComparison.OrdinalIgnoreCase)
            ? Visibility.Visible : Visibility.Collapsed;
    public object ConvertBack(object value, Type t, object p, CultureInfo c)
        => throw new NotSupportedException();
}

/// <summary>true → expanded sidebar width, false → collapsed (icon-only) width.</summary>
public class BoolToSidebarWidthConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c)
        => new GridLength(value is bool b && b ? 248 : 76);
    public object ConvertBack(object value, Type t, object p, CultureInfo c)
        => throw new NotSupportedException();
}

/// <summary>
/// Maps a status string/enum to a pill colour. ConverterParameter "bg" returns the
/// soft background; anything else returns the strong text colour.
/// </summary>
public class StatusBrushConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c)
    {
        string s = value?.ToString()?.ToLowerInvariant() ?? "";
        string cat;
        if (s is "rejected" or "cancelled" or "inactive" || s.Contains("out of stock"))
            cat = "red";
        else if (s.Contains("low") || s is "pending" or "packed" or "collected" or "received"
                 or "cleaning" or "drying" or "grinding" or "packaging" or "fair")
            cat = "amber";
        else if (s is "dispatched" || s.Contains("transit"))
            cat = "blue";
        else
            cat = "green"; // active, ok, passed, completed, delivered, confirmed, stored, in stock

        bool bg = p?.ToString() == "bg";
        (Color strong, Color soft) = cat switch
        {
            "red" => (Color.FromRgb(0xC0, 0x39, 0x2B), Color.FromRgb(0xF8, 0xE2, 0xDF)),
            "amber" => (Color.FromRgb(0xB9, 0x73, 0x0E), Color.FromRgb(0xFB, 0xEF, 0xD2)),
            "blue" => (Color.FromRgb(0x2E, 0x6F, 0xA8), Color.FromRgb(0xDD, 0xEA, 0xF6)),
            _ => (Color.FromRgb(0x2E, 0x7D, 0x32), Color.FromRgb(0xE2, 0xF1, 0xDF)),
        };
        return new SolidColorBrush(bg ? soft : strong);
    }
    public object ConvertBack(object value, Type t, object p, CultureInfo c)
        => throw new NotSupportedException();
}

/// <summary>Greater-than-zero count → Visible (for badge dots).</summary>
public class CountToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c)
    {
        bool hasItems = value is int n && n > 0;
        if (p?.ToString() == "invert") hasItems = !hasItems;
        return hasItems ? Visibility.Visible : Visibility.Collapsed;
    }
    public object ConvertBack(object value, Type t, object p, CultureInfo c)
        => throw new NotSupportedException();
}
