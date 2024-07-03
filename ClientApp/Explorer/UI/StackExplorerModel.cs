using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Thetacat.Model;

namespace Thetacat.Explorer.UI;

/*----------------------------------------------------------------------------
    %%Class: StackExplorerModel
    %%Qualified: Thetacat.Explorer.UI.StackExplorerModel

    This holds a single stack explorer (showing a stack of media items)
----------------------------------------------------------------------------*/
public class StackExplorerModel: INotifyPropertyChanged
{
    public MediaExplorerCollection ExplorerCollection { get; } = new(14.0);

    public double PanelItemHeight
    {
        get => m_panelItemHeight;
        set => SetField(ref m_panelItemHeight, value);
    }

    public double PanelItemWidth
    {
        get => m_panelItemWidth;
        set => SetField(ref m_panelItemWidth, value);
    }

    public double ImageHeight
    {
        get => m_imageHeight;
        set => SetField(ref m_imageHeight, value);
    }

    public double ImageWidth
    {
        get => m_imageWidth;
        set => SetField(ref m_imageWidth, value);
    }

    private double m_panelItemHeight = 112.0;
    private double m_panelItemWidth = 148.8;
    private double m_imageHeight = 96.0;
    private double m_imageWidth = 148.0;

    public StackExplorerModel()
    {
    }

    public void PopulateCollectionForItemInStack(MediaItem itemInStack)
    {

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
