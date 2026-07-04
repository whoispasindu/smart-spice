using SmartSpice.Models;

namespace SmartSpice.Services;

/// <summary>
/// Holds the currently authenticated user for the lifetime of the app session.
/// A single shared instance is created at startup (lightweight service locator).
/// </summary>
public class AppSession
{
    public Employee? CurrentUser { get; set; }

    public bool IsLoggedIn => CurrentUser != null;

    public string CurrentUserName => CurrentUser?.FullName ?? "Guest";

    public UserRole CurrentRole => CurrentUser?.Role ?? UserRole.WarehouseStaff;

    /// <summary>Role-based access check used to show/hide privileged features.</summary>
    public bool IsAdmin => CurrentRole == UserRole.Administrator;
    public bool IsSales => CurrentRole == UserRole.SalesOfficer;
    public bool IsQuality => CurrentRole == UserRole.QualityInspector;

    /// <summary>Admins may add/edit master data.</summary>
    public bool CanManage => IsAdmin;

    /// <summary>Only these three roles may use the system.</summary>
    public static bool RoleHasAccess(UserRole role) =>
        role is UserRole.Administrator or UserRole.SalesOfficer or UserRole.QualityInspector;

    /// <summary>
    /// Which navigation pages this user may open. Admin = everything;
    /// Sales = only Dispatch Products; Quality Inspector = only Quality Control.
    /// </summary>
    public bool CanAccess(string pageKey) => CurrentRole switch
    {
        UserRole.Administrator => true,
        UserRole.SalesOfficer => pageKey == "sales",
        UserRole.QualityInspector => pageKey == "quality",
        _ => false
    };

    /// <summary>The page each role lands on after login.</summary>
    public string DefaultPage => CurrentRole switch
    {
        UserRole.SalesOfficer => "sales",
        UserRole.QualityInspector => "quality",
        _ => "dashboard"
    };
}
