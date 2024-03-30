using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Thetacat.Types;
using Thetacat.Util;

namespace Thetacat.Import.UI;

public class RepathNode : INotifyPropertyChanged, ICheckableTreeViewItem<RepathNode>
{
    public bool Checked { get; set; }
    public PathSegment Leaf { get; set; }
    public PathSegment FullPath { get; set; }

    public ObservableCollection<RepathNode> Children { get; set; } = new();

    public RepathNode(PathSegment path)
    {
        FullPath = new PathSegment(path);
        Leaf = path.GetLeafItem() ?? PathSegment.Empty;
    }

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
