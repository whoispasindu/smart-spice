using System.Windows;
using System.Windows.Controls;
using SmartSpice.ViewModels;

namespace SmartSpice.Views;

public partial class SettingsView : UserControl
{
    public SettingsView() => InitializeComponent();

    private void ChangePassword_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not SettingsViewModel vm) return;
        vm.ChangePassword(CurrentPwd.Password, NewPwd.Password, ConfirmPwd.Password);
        CurrentPwd.Clear();
        NewPwd.Clear();
        ConfirmPwd.Clear();
    }
}
