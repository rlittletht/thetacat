using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Thetacat.Explorer.Commands;
using Thetacat.Filtering.UI;
using Thetacat.Metatags.Model;

namespace Thetacat.Metatags;

public class ManageMetadataModel: INotifyPropertyChanged
{
    public ObservableCollection<FilterModelMetatagItem> AvailableParents { get; set; } = new ObservableCollection<FilterModelMetatagItem>();
    public ManageMetadataMetatag? MetatagBase;

    private ManageMetadataMetatag? m_selectedMetatag;
    private MetatagTreeItem? m_selectedTreeItem;
    private FilterModelMetatagItem? m_currentParent;

    public FilterModelMetatagItem? CurrentParent
    {
        get => m_currentParent;
        set => SetField(ref m_currentParent, value);
    }

    public MetatagTreeItem? SelectedTreeItem
    {
        get => m_selectedTreeItem;
        set => SetField(ref m_selectedTreeItem, value);
    }

    public ManageMetadataMetatag? SelectedMetatag
    {
        get => m_selectedMetatag;
        set => SetField(ref m_selectedMetatag, value);
    }

    public DeleteMetatagCommand? DeleteMetatagCommand { get; set; }
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
