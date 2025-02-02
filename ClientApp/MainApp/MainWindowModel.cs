using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Thetacat.Explorer;
using Thetacat.Filtering;
using Thetacat.Model;
using Thetacat.TcSettings;

namespace Thetacat.MainApp;

public class MainWindowModel: INotifyPropertyChanged
{
    private bool m_isExplorerCollectionDirty;
    private bool m_isSchemaDirty;
    private Profile? m_currentProfile;
    private ExplorerItemSize m_itemSize = ExplorerItemSize.Medium;
    private bool m_isQuickFilterPanelVisible = false;
    
    public MediaExplorerCollection ExplorerCollection { get; } = new(14.0);
    public ObservableCollection<Filter> AvailableFilters { get; } = new ();
    public ObservableCollection<Profile> AvailableProfiles { get; } = new ();

    public bool IsExtraLargePreview => m_itemSize.Equals(ExplorerItemSize.ExtraLarge);
    public bool IsLargePreview => m_itemSize.Equals(ExplorerItemSize.Large);
    public bool IsMediumPreview => m_itemSize.Equals(ExplorerItemSize.Medium);
    public bool IsSmallPreview => m_itemSize.Equals(ExplorerItemSize.Small);

    public bool IsQuickFilterPanelVisible
    {
        get => m_isQuickFilterPanelVisible;
        set => SetField(ref m_isQuickFilterPanelVisible, value);
    }

    public ExplorerItemSize ItemSize
    {
        get => m_itemSize;
        set
        {
            SetField(ref m_itemSize, value);
            OnPropertyChanged(nameof(IsExtraLargePreview));
            OnPropertyChanged(nameof(IsLargePreview));
            OnPropertyChanged(nameof(IsMediumPreview));
            OnPropertyChanged(nameof(IsSmallPreview));
        }
    }
    public Profile? CurrentProfile
    {
        get => m_currentProfile;
        set => SetField(ref m_currentProfile, value);
    }

    public bool IsExplorerCollectionDirty
    {
        get => m_isExplorerCollectionDirty;
        set
        {
            if (SetField(ref m_isExplorerCollectionDirty, value))
            {
                OnPropertyChanged(nameof(IsExplorerCollectionDirty));
                OnPropertyChanged(nameof(IsDirty));
            }
        }
    }

    public bool IsSchemaDirty
    {
        get => m_isSchemaDirty;
        set
        {
            if (SetField(ref m_isSchemaDirty, value))
            {
                OnPropertyChanged(nameof(IsSchemaDirty));
                OnPropertyChanged(nameof(IsDirty));
            }
        }
    }

    public bool IsDirty => IsExplorerCollectionDirty || IsSchemaDirty;

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
