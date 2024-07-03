using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Thetacat.Model;

namespace Thetacat.Explorer.UI;

public class SelectStackModel: INotifyPropertyChanged
{
    private MediaStack? m_currentStack;
    private string m_description = "";
    private string m_stackId = "";
    private MediaStackType m_currentType = MediaStackType.Media;

    public ObservableCollection<MediaStack> AvailableStacks { get; } = new ObservableCollection<MediaStack>();
    public ObservableCollection<MediaStackType> StackTypes { get; set; } = new(new[] { MediaStackType.Media, MediaStackType.Version });

    public bool IsDetailsEditable => m_currentStack == null;

    public MediaStack? CurrentStack
    {
        get => m_currentStack;
        set
        {
            SetField(ref m_currentStack, value);
            OnPropertyChanged(nameof(IsDetailsEditable));
        }
    }

    public string Description
    {
        get => m_description;
        set => SetField(ref m_description, value);
    }

    public string StackId
    {
        get => m_stackId;
        set => SetField(ref m_stackId, value);
    }


    public MediaStackType CurrentType
    {
        get => m_currentType;
        set => SetField(ref m_currentType, value);
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
