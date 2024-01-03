using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Thetacat.Metatags;
using Thetacat.Types;

namespace Thetacat.Controls;

public class CheckableMetatagTreeItem : IMetatagTreeItem, INotifyPropertyChanged
{
    public ObservableCollection<IMetatagTreeItem> Children => m_itemInner.Children;
    public string Description => m_itemInner.Description;
    public string Name => m_itemInner.Name;
    public string ID => m_itemInner.ID;

    public IMetatagTreeItem? FindMatchingChild(IMetatagMatcher<IMetatagTreeItem> matcher, int levelsToRecurse) =>
        m_itemInner.FindMatchingChild(matcher, levelsToRecurse);

    public void SeekAndDelete(HashSet<string> delete) => m_itemInner.SeekAndDelete(delete);
    public bool FilterTreeToMatches(MetatagTreeItemMatcher matcher) => m_itemInner.FilterTreeToMatches(matcher);

    public bool Checked
    {
        get => m_checked;
        set => SetField(ref m_checked, value);
    }

    private readonly IMetatagTreeItem m_itemInner;
    private bool m_checked;

    public CheckableMetatagTreeItem(IMetatagTreeItem item)
    {
        m_itemInner = item;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
