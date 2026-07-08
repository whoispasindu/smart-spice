using CommunityToolkit.Mvvm.ComponentModel;

namespace SmartSpice.ViewModels;

/// Implemented by list pages so the global search box can filter them live.
public interface ISearchable
{
    void ApplySearch(string? text);
}

/// Shared base for all view-models. Inherits change-notification from the MVVM
/// toolkit's <see cref="ObservableObject"/> and adds a common screen title.

public abstract partial class ViewModelBase : ObservableObject
{
    [ObservableProperty]
    private string _title = string.Empty;

    /// Override to (re)load data when the view becomes active.
    public virtual void Load() { }
}
