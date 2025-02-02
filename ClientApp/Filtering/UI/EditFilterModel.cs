using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using TCore.PostfixText;
using Thetacat.Metatags.Model;

namespace Thetacat.Filtering.UI;

public class EditFilterModel: INotifyPropertyChanged
{
    public ObservableCollection<FilterModelMetatagItem> AvailableTags { get; set; } = new ObservableCollection<FilterModelMetatagItem>();
    public ObservableCollection<ComparisonOperator> ComparisonOperators { get; set; } = new();
    public ObservableCollection<PostfixOperator> PostfixOperators { get; set; } = new();
    public ObservableCollection<string> ValuesForClause { get; set; } = new ObservableCollection<string>();

    public ObservableCollection<string> ExpressionClauses { get; set; } = new();

    public ObservableCollection<string> Types { get; set; } = new();

    public Visibility ExpressionEditorVisibility => m_isEditingExpression ? Visibility.Visible : Visibility.Collapsed;
    public Visibility ExpressionViewVisibility => m_isEditingExpression ? Visibility.Collapsed: Visibility.Visible;

    public string ExpressionEditing
    {
        get => m_expressionEditing;
        set => SetField(ref m_expressionEditing, value);
    }

    public bool IsEditingExpression
    {
        get => m_isEditingExpression;
        set
        {
            SetField(ref m_isEditingExpression, value);
            OnPropertyChanged(nameof(ExpressionEditorVisibility));
            OnPropertyChanged(nameof(ExpressionViewVisibility));
        }
    }

    public string SelectedType
    {
        get => m_selectedType;
        set => SetField(ref m_selectedType, value);
    }

    public bool IsTypeAvailable
    {
        get => m_isTypeAvailable;
        set => SetField(ref m_isTypeAvailable, value);
    }

    public Guid Id
    {
        get => m_id;
        set => SetField(ref m_id, value);
    }

    private FilterModelMetatagItem? m_selectedTagForClause;
    private ComparisonOperator? m_comparisonOpForClause;
    private string m_valueForClause = string.Empty;
    private PostfixOperator m_postfixOpForClause;
    private string m_filterName = string.Empty;
    private string m_description = string.Empty;
    private string m_valueTextForClause;
    private string m_selectedType = "Local";
    private bool m_isTypeAvailable;
    private Guid m_id;
    private bool m_isEditingExpression = false;
    private string m_expressionEditing = "";

    public PostfixText Expression { get; set; } = new PostfixText();

    public string ValueTextForClause
    {
        get => m_valueTextForClause;
        set => SetField(ref m_valueTextForClause, value);
    }

    public FilterModelMetatagItem? SelectedTagForClause
    {
        get => m_selectedTagForClause;
        set => SetField(ref m_selectedTagForClause, value);
    }

    public ComparisonOperator? ComparisonOpForClause
    {
        get => m_comparisonOpForClause;
        set => SetField(ref m_comparisonOpForClause, value);
    }

    public string ValueForClause
    {
        get => m_valueForClause;
        set => SetField(ref m_valueForClause, value);
    }

    public PostfixOperator PostfixOpForClause
    {
        get => m_postfixOpForClause;
        set => SetField(ref m_postfixOpForClause, value);
    }

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

    public EditFilterModel()
    {
        m_postfixOpForClause = new PostfixOperator(PostfixOperator.Op.And);
        m_valueTextForClause = string.Empty;

        PostfixOperators.Add(m_postfixOpForClause);
        PostfixOperators.Add(new PostfixOperator(PostfixOperator.Op.Or));
    }
}
