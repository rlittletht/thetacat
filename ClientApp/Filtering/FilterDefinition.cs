using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using TCore.PostfixText;
using Thetacat.Model;

namespace Thetacat.Filtering;

public class FilterDefinition: INotifyPropertyChanged
{
    public string FilterName
    {
        get => m_filterName;
        set => SetField(ref m_filterName, value);
    }

    public string Description
    {
        get => m_description;
        set => SetField(ref m_description, value);
    }

    public PostfixText Expression;
    private string m_filterName;
    private string m_description;

    public FilterDefinition()
    {
        m_filterName = "";
        m_description = "";
        Expression = new PostfixText();
    }

    public string ExpressionText
    {
        get => Expression.ToString();
        set => Expression = PostfixText.CreateFromParserClient(new StringParserClient(value));
    }

    public FilterDefinition(string name, string description, string filterExpression)
    {
        m_filterName = name;
        Expression = PostfixText.CreateFromParserClient(new StringParserClient(filterExpression));
        m_description = description;
    }

    public override string ToString() => m_filterName;

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
