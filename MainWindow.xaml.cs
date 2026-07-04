using System.Windows;
using SmartSpice.Services;
using SmartSpice.ViewModels;
using SmartSpice.Views;

namespace SmartSpice;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }

    private void Logout_Click(object sender, RoutedEventArgs e)
    {
        ServiceHub.Auth.Logout();
        var login = new LoginWindow();
        login.Show();
        Close();
    }
}
