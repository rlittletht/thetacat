using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using TCore.PostfixText;
using Thetacat.Metatags.Model;

namespace Thetacat.Filtering.UI;

public class EditFilterModel: INotifyPropertyChanged
{
    public ObservableCollection<FilterModelMetatagItem> AvailableTags { get; set; } = new ObservableCollection<FilterModelMetatagItem>();
    public ObservableCollection<ComparisonOperator> ComparisonOperators { get; set; } = new();
    public ObservableCollection<PostfixOperator> PostfixOperators { get; set; } = new();
    public ObservableCollection<string> ValuesForClause { get; set; } = new ObservableCollection<string>() { "$true", "$false" };

    public ObservableCollection<string> ExpressionClauses { get; set; } = new();

    private FilterModelMetatagItem? m_selectedTagForClause;
    private ComparisonOperator m_comparisonOpForClause;
    private string m_valueForClause = string.Empty;
    private PostfixOperator m_postfixOpForClause;
    private string m_filterName = string.Empty;
    private string m_description = string.Empty;

    public PostfixText Expression { get; set; } = new PostfixText();

    public FilterModelMetatagItem? SelectedTagForClause
    {
        get => m_selectedTagForClause;
        set => SetField(ref m_selectedTagForClause, value);
    }

    public ComparisonOperator ComparisonOpForClause
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
        m_comparisonOpForClause = new ComparisonOperator(ComparisonOperator.Op.Eq);
        ComparisonOperators.Add(m_comparisonOpForClause);
        ComparisonOperators.Add(new ComparisonOperator(ComparisonOperator.Op.Ne));

        m_postfixOpForClause = new PostfixOperator(PostfixOperator.Op.And);

        PostfixOperators.Add(m_postfixOpForClause);
        PostfixOperators.Add(new PostfixOperator(PostfixOperator.Op.Or));
    }
}
