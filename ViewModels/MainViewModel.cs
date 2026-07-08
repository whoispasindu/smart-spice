using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartSpice.Services;

namespace SmartSpice.ViewModels;

/// The shell view-model. Owns the currently displayed page and the navigation
/// commands, plus header info (current user, role, unread notifications).

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private ViewModelBase? _currentPage;

    [ObservableProperty]
    private string _activeKey = "dashboard";

    [ObservableProperty]
    private int _unreadCount;

    [ObservableProperty]
    private bool _isSidebarExpanded = true;

    [ObservableProperty]
    private string _searchText = string.Empty;

    partial void OnSearchTextChanged(string value)
        => (CurrentPage as ISearchable)?.ApplySearch(value);

    [RelayCommand]
    private void ToggleSidebar() => IsSidebarExpanded = !IsSidebarExpanded;

    public string UserName => ServiceHub.Session.CurrentUserName;
    public string UserRole => ServiceHub.Session.CurrentRole.ToString();
    public string UserInitials =>
        string.Concat(UserName.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Take(2).Select(w => w[0])).ToUpper();

    // Per-role navigation visibility 
    private AppSession Session => ServiceHub.Session;
    public bool ShowDashboard => Session.CanAccess("dashboard");
    public bool ShowFarmers => Session.CanAccess("farmers");
    public bool ShowEmployees => Session.CanAccess("employees");
    public bool ShowBatches => Session.CanAccess("batches");
    public bool ShowQuality => Session.CanAccess("quality");
    public bool ShowInventory => Session.CanAccess("inventory");
    public bool ShowSales => Session.CanAccess("sales");
    public bool ShowWarehouses => Session.CanAccess("warehouses");
    public bool ShowForecast => Session.CanAccess("forecast");
    public bool ShowNotifications => Session.CanAccess("notifications");
    public bool ShowAudit => Session.CanAccess("audit");
    public bool ShowSettings => Session.CanAccess("settings");

    public MainViewModel()
    {
        Navigate(Session.DefaultPage);
    }

    [RelayCommand]
    private void Navigate(string key)
    {
        
        if (!Session.CanAccess(key)) return;

        ActiveKey = key;
        CurrentPage = key switch
        {
            "dashboard" => new DashboardViewModel(),
            "inventory" => new InventoryViewModel(),
            "farmers" => new FarmersViewModel(),
            "batches" => new BatchesViewModel(),
            "quality" => new QualityViewModel(),
            "sales" => new SalesViewModel(),
            "warehouses" => new WarehousesViewModel(),
            "employees" => new EmployeesViewModel(),
            "forecast" => new SalesForecastViewModel(),
            "notifications" => new NotificationsViewModel(),
            "audit" => new ReportsViewModel(),
            "settings" => new SettingsViewModel(),
            _ => CurrentPage
        };
        CurrentPage?.Load();
        if (!string.IsNullOrEmpty(SearchText))
            SearchText = string.Empty; 
    }

    public void RefreshBadges()
    {
        UnreadCount = ServiceHub.Notifications.UnreadCount();
    }
}
