using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Thetacat.Model;

namespace Thetacat;

public class MainWindowModel: INotifyPropertyChanged
{
    private bool m_isExplorerCollectionDirty;
    private bool m_isSchemaDirty;
    public MediaExplorerCollection ExplorerCollection { get; } = new(14.0);

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
