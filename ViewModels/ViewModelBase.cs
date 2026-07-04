using CommunityToolkit.Mvvm.ComponentModel;

namespace SmartSpice.ViewModels;

/// <summary>Implemented by list pages so the global search box can filter them live.</summary>
public interface ISearchable
{
    void ApplySearch(string? text);
}

/// <summary>
/// Shared base for all view-models. Inherits change-notification from the MVVM
/// toolkit's <see cref="ObservableObject"/> and adds a common screen title.
/// </summary>
public abstract partial class ViewModelBase : ObservableObject
{
    [ObservableProperty]
    private string _title = string.Empty;

    /// <summary>Override to (re)load data when the view becomes active.</summary>
    public virtual void Load() { }
}
