using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Thetacat.UI;

public class ProgressDialogModel: INotifyPropertyChanged
{
    private int m_progressValue = 0;
    private bool m_isIndeterminate = false;

    public bool IsIndeterminate
    {
        get => m_isIndeterminate;
        set => SetField(ref m_isIndeterminate, value);
    }

    public int ProgressValue
    {
        get => m_progressValue;
        set => SetField(ref m_progressValue, value);
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
