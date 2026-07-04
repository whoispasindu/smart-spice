using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartSpice.Data;
using SmartSpice.Helpers;
using SmartSpice.Models;
using SmartSpice.Services;

namespace SmartSpice.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    // section switch: "profile" | "password" | "users"
    [ObservableProperty] private string _activeSection = "profile";

    public bool IsAdmin => ServiceHub.Session.IsAdmin || ServiceHub.Session.CanManage;

    // ---- Profile ----
    [ObservableProperty] private string _profileName = string.Empty;
    [ObservableProperty] private string _profileEmail = string.Empty;
    [ObservableProperty] private string _profilePhone = string.Empty;
    public string RoleText => ServiceHub.Session.CurrentRole.ToString();
    public string EmployeeCode => ServiceHub.Session.CurrentUser?.EmployeeCode ?? "—";
    public string Department => ServiceHub.Session.CurrentUser?.Department ?? "—";
    public string UsernameText => ServiceHub.Session.CurrentUser?.Username ?? "—";
    public string Initials
    {
        get
        {
            var name = ProfileName;
            if (string.IsNullOrWhiteSpace(name)) return "?";
            var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return string.Concat(parts.Take(2).Select(p => p[0])).ToUpper();
        }
    }
    public string LastLoginText =>
        ServiceHub.Session.CurrentUser?.LastLoginAt?.ToString("dd MMM yyyy - hh:mm tt") ?? "—";
    public string StatusText => (ServiceHub.Session.CurrentUser?.IsActive ?? false) ? "Active" : "Inactive";

    public SettingsViewModel() => Title = "Settings";

    public override void Load()
    {
        var u = ServiceHub.Session.CurrentUser;
        ProfileName = u?.FullName ?? string.Empty;
        ProfileEmail = u?.Email ?? string.Empty;
        ProfilePhone = u?.Phone ?? string.Empty;
        OnPropertyChanged(nameof(Initials));
    }

    [RelayCommand]
    private void SelectSection(string section) => ActiveSection = section;

    // ---------- Profile ----------
    [RelayCommand]
    private void UpdateProfile()
    {
        if (string.IsNullOrWhiteSpace(ProfileName))
        {
            MessageBox.Show("Name cannot be empty.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        var id = ServiceHub.Session.CurrentUser?.Id ?? 0;
        using (var db = new SmartSpiceContext())
        {
            var e = db.Employees.Find(id);
            if (e == null) return;
            e.FullName = ProfileName.Trim();
            e.Email = ProfileEmail.Trim();
            e.Phone = ProfilePhone.Trim();
            db.SaveChanges();

            // keep the in-memory session in sync so the header updates next navigation
            if (ServiceHub.Session.CurrentUser != null)
            {
                ServiceHub.Session.CurrentUser.FullName = e.FullName;
                ServiceHub.Session.CurrentUser.Email = e.Email;
                ServiceHub.Session.CurrentUser.Phone = e.Phone;
            }
        }
        ServiceHub.Audit.Log("UPDATE", "Profile", $"{ProfileName} updated their profile");
        OnPropertyChanged(nameof(Initials));
        MessageBox.Show("Profile updated successfully.", "Settings", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    // ---------- Password (called from code-behind with PasswordBox values) ----------
    public void ChangePassword(string current, string @new, string confirm)
    {
        if (string.IsNullOrWhiteSpace(current) || string.IsNullOrWhiteSpace(@new))
        {
            MessageBox.Show("Please fill in all password fields.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (@new.Length < 6)
        {
            MessageBox.Show("New password must be at least 6 characters.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (@new != confirm)
        {
            MessageBox.Show("New password and confirmation do not match.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var id = ServiceHub.Session.CurrentUser?.Id ?? 0;
        using var db = new SmartSpiceContext();
        var e = db.Employees.Find(id);
        if (e == null) return;
        if (!PasswordHasher.Verify(current, e.PasswordHash))
        {
            MessageBox.Show("Current password is incorrect.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        e.PasswordHash = PasswordHasher.Hash(@new);
        db.SaveChanges();
        if (ServiceHub.Session.CurrentUser != null)
            ServiceHub.Session.CurrentUser.PasswordHash = e.PasswordHash;
        ServiceHub.Audit.Log("SECURITY", "Password", $"{e.Username} changed their password");
        MessageBox.Show("Password changed successfully.", "Settings", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
