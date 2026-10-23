using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Thetacat.Triage;

public class DownloadMediaModel: INotifyPropertyChanged
{
    private string m_downloadTo = string.Empty;
    private bool m_triage;
    
    public bool Triage
    {
        get => m_triage;
        set => SetField(ref m_triage, value);
    }

    public string DownloadTo
    {
        get => m_downloadTo;
        set => SetField(ref m_downloadTo, value);
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
