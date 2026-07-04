using System.Windows;
using SmartSpice.Data;
using SmartSpice.Views;

namespace SmartSpice;

/// <summary>
/// Application entry point: initialises the database then shows the login window.
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            using var db = new SmartSpiceContext();
            DbSeeder.EnsureSeeded(db);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to initialise the database:\n\n{ex.Message}",
                "SmartSpice", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
            return;
        }

        var login = new LoginWindow();
        login.Show();
    }
}
