using System.IO;
using System.Text;
using System.Windows;
using Microsoft.Win32;

namespace SmartSpice.Helpers;

/// <summary>
/// Exports a tabular report to a CSV file the user chooses via a Save dialog.
/// </summary>
public static class CsvExporter
{
    public static void Export(string suggestedFileName, IList<string> headers, IEnumerable<IList<string>> rows)
    {
        var dlg = new SaveFileDialog
        {
            FileName = suggestedFileName,
            Filter = "CSV file (*.csv)|*.csv",
            DefaultExt = ".csv",
            AddExtension = true
        };
        if (dlg.ShowDialog() != true) return;

        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",", headers.Select(Escape)));
        foreach (var row in rows)
            sb.AppendLine(string.Join(",", row.Select(Escape)));

        try
        {
            File.WriteAllText(dlg.FileName, sb.ToString(), Encoding.UTF8);
            MessageBox.Show($"Report exported to:\n{dlg.FileName}", "Export complete",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not save the report:\n{ex.Message}", "Export failed",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static string Escape(string? s)
    {
        s ??= string.Empty;
        return s.Contains(',') || s.Contains('"') || s.Contains('\n')
            ? "\"" + s.Replace("\"", "\"\"") + "\""
            : s;
    }
}

