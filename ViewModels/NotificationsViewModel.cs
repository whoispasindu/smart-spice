using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartSpice.Models;
using SmartSpice.Services;

namespace SmartSpice.ViewModels;

public partial class NotificationsViewModel : ViewModelBase
{
    public ObservableCollection<Notification> Notifications { get; } = new();

    [ObservableProperty] private string _summary = string.Empty;

    public NotificationsViewModel() => Title = "Notifications";

    public override void Load()
    {
        // Re-scan inventory so freshly-created shortages show up here.
        ServiceHub.Notifications.RefreshLowStockAlerts();
        Refresh();
    }

    private void Refresh()
    {
        Notifications.Clear();
        foreach (var n in ServiceHub.Notifications.GetAll()) Notifications.Add(n);
        int unread = Notifications.Count(n => !n.IsRead);
        Summary = $"{Notifications.Count} alerts   •   {unread} unread";
    }

    [RelayCommand]
    private void MarkAllRead()
    {
        ServiceHub.Notifications.MarkAllRead();
        Refresh();
    }
}
