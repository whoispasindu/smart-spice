using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartSpice.Data;
using SmartSpice.Helpers;
using SmartSpice.Models;
using SmartSpice.Services;

namespace SmartSpice.ViewModels;

public partial class EmployeesViewModel : ViewModelBase, ISearchable
{
    private List<Employee> _all = new();
    private string _search = string.Empty;

    public ObservableCollection<Employee> Employees { get; } = new();
    public Array Roles => Enum.GetValues(typeof(UserRole));

    /// <summary>Only managers/admins may add or edit staff records.</summary>
    public bool CanManage => ServiceHub.Session.CanManage;

    [ObservableProperty] private int _totalEmployees;
    [ObservableProperty] private int _activeEmployees;
    [ObservableProperty] private int _departmentCount;
    [ObservableProperty] private int _onLeaveEmployees;

    // Add / edit
    [ObservableProperty] private Employee? _selected;
    [ObservableProperty] private bool _isEditing;
    [ObservableProperty] private string _newPassword = string.Empty;

    // Detail popup
    [ObservableProperty] private bool _isDetail;
    [ObservableProperty] private Employee? _detail;

    public EmployeesViewModel() => Title = "Employees";

    public override void Load() => Refresh();

    private void Refresh()
    {
        using (var db = new SmartSpiceContext())
            _all = db.Employees.OrderBy(e => e.FullName).ToList();

        TotalEmployees = _all.Count;
        ActiveEmployees = _all.Count(e => e.IsActive);
        OnLeaveEmployees = _all.Count(e => !e.IsActive);
        DepartmentCount = _all.Select(e => e.Department)
            .Where(d => !string.IsNullOrWhiteSpace(d))
            .Distinct(StringComparer.OrdinalIgnoreCase).Count();
        ApplyFilter();
    }

    public void ApplySearch(string? text)
    {
        _search = text?.Trim() ?? string.Empty;
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        IEnumerable<Employee> q = _all;
        if (_search.Length > 0)
            q = q.Where(e =>
                e.FullName.Contains(_search, StringComparison.OrdinalIgnoreCase) ||
                e.EmployeeCode.Contains(_search, StringComparison.OrdinalIgnoreCase) ||
                e.Department.Contains(_search, StringComparison.OrdinalIgnoreCase) ||
                e.Role.ToString().Contains(_search, StringComparison.OrdinalIgnoreCase) ||
                e.Phone.Contains(_search, StringComparison.OrdinalIgnoreCase));

        Employees.Clear();
        foreach (var e in q) Employees.Add(e);
    }

    // ---------- detail popup ----------
    [RelayCommand]
    private void OpenDetail(Employee? emp)
    {
        if (emp == null) return;
        Detail = emp;
        IsDetail = true;
    }

    [RelayCommand] private void CloseDetail() => IsDetail = false;

    // ---------- add / edit ----------
    [RelayCommand]
    private void New()
    {
        if (!CanManage) return;
        Selected = new Employee { Role = UserRole.WarehouseStaff, IsActive = true, Department = "Operations", HireDate = DateTime.Now };
        NewPassword = "spice123";
        IsEditing = true;
    }

    [RelayCommand]
    private void EditRow(Employee? emp)
    {
        if (emp == null || !CanManage) return;
        Selected = Clone(emp);
        NewPassword = string.Empty; // blank = keep existing password
        IsEditing = true;
    }

    [RelayCommand]
    private void Save()
    {
        if (Selected == null) return;
        if (string.IsNullOrWhiteSpace(Selected.FullName) || string.IsNullOrWhiteSpace(Selected.Username))
        {
            MessageBox.Show("Full name and username are required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        try
        {
            using var db = new SmartSpiceContext();
            if (Selected.Id == 0)
            {
                if (db.Employees.Any(e => e.Username == Selected.Username))
                {
                    MessageBox.Show("That username is already taken.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (string.IsNullOrWhiteSpace(NewPassword)) NewPassword = "spice123";
                Selected.PasswordHash = PasswordHasher.Hash(NewPassword);
                if (string.IsNullOrWhiteSpace(Selected.EmployeeCode))
                    Selected.EmployeeCode = $"EMP-{db.Employees.Count() + 1:000}";
                db.Employees.Add(Selected);
            }
            else
            {
                var e = db.Employees.Find(Selected.Id)!;
                e.FullName = Selected.FullName;
                e.Username = Selected.Username;
                e.EmployeeCode = Selected.EmployeeCode;
                e.Role = Selected.Role;
                e.Department = Selected.Department;
                e.Phone = Selected.Phone;
                e.Email = Selected.Email;
                e.Address = Selected.Address;
                e.HireDate = Selected.HireDate;
                e.IsActive = Selected.IsActive;
                if (!string.IsNullOrWhiteSpace(NewPassword))
                    e.PasswordHash = PasswordHasher.Hash(NewPassword);
            }
            db.SaveChanges();
            ServiceHub.Audit.Log(Selected.Id == 0 ? "CREATE" : "UPDATE", "Employee", $"{Selected.FullName} ({Selected.Role})");
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        IsEditing = false;
        Selected = null;
        Refresh();
    }

    [RelayCommand]
    private void Cancel()
    {
        IsEditing = false;
        Selected = null;
    }

    private static Employee Clone(Employee s) => new()
    {
        Id = s.Id,
        FullName = s.FullName,
        Username = s.Username,
        EmployeeCode = s.EmployeeCode,
        Role = s.Role,
        Department = s.Department,
        Phone = s.Phone,
        Email = s.Email,
        Address = s.Address,
        HireDate = s.HireDate,
        IsActive = s.IsActive,
        PasswordHash = s.PasswordHash,
        LastLoginAt = s.LastLoginAt,
        CreatedAt = s.CreatedAt
    };
}
