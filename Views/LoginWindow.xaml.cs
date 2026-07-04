using SmartSpice.Services;
using System.Security.Authentication;
using System.Windows;
using System.Windows.Input;

namespace SmartSpice.Views;

public partial class LoginWindow : Window
{
    public LoginWindow()
    {
        InitializeComponent();
        UsernameBox.Focus();
        UsernameBox.SelectAll();
    }

    private void SignIn_Click(object sender, RoutedEventArgs e) => TryLogin();

    private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) TryLogin();
    }

    private void TryLogin()
    {
        ErrorText.Visibility = Visibility.Collapsed;
        try
        {
            var user = ServiceHub.Auth.Login(UsernameBox.Text, PasswordBox.Password);
            if (user == null) return;

            var main = new MainWindow();
            main.Show();
            Close();
        }
        catch (AuthenticationException ex)
        {
            ErrorText.Text = ex.Message;
            ErrorText.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            ErrorText.Text = "Unexpected error: " + ex.Message;
            ErrorText.Visibility = Visibility.Visible;
        }
    }
}
