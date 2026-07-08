using SmartSpice.Models;

namespace SmartSpice.Services;

public class AppSession
{
    public Employee? CurrentUser { get; set; }

    public bool IsLoggedIn => CurrentUser != null;

    public string CurrentUserName => CurrentUser?.FullName ?? "Guest";

    public UserRole CurrentRole => CurrentUser?.Role ?? UserRole.WarehouseStaff;

    public bool IsAdmin => CurrentRole == UserRole.Administrator;
    public bool IsSales => CurrentRole == UserRole.SalesOfficer;
    public bool IsQuality => CurrentRole == UserRole.QualityInspector;

    public bool CanManage => IsAdmin;

    public static bool RoleHasAccess(UserRole role) =>
        role is UserRole.Administrator or UserRole.SalesOfficer or UserRole.QualityInspector;


    public bool CanAccess(string pageKey) => CurrentRole switch
    {
        UserRole.Administrator => true,
        UserRole.SalesOfficer => pageKey == "sales",
        UserRole.QualityInspector => pageKey == "quality",
        _ => false
    };

    public string DefaultPage => CurrentRole switch
    {
        UserRole.SalesOfficer => "sales",
        UserRole.QualityInspector => "quality",
        _ => "dashboard"
    };
}
