using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Thetacat.UI.Input;

public class InputBoxModel: INotifyPropertyChanged
{
    private string m_inputText = string.Empty;
    private string m_prompt = string.Empty;

    public string Prompt
    {
        get => m_prompt;
        set => SetField(ref m_prompt, value);
    }

    public string InputText
    {
        get => m_inputText;
        set => SetField(ref m_inputText, value);
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
