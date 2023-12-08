using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Thetacat.Model;

namespace Thetacat.Migration.Elements.Metadata.UI;

public class MetatagMigrationItem: INotifyPropertyChanged
{
    private string m_opType;
    private bool m_include;

    public MetatagSchemaDiffOp DiffOp { get; init; }
    public string Details => DiffOp.ToString();

    public bool Include
    {
        get => m_include;
        set => SetField(ref m_include, value);
    }

    public string OpType
    {
        get => m_opType;
        set => SetField(ref m_opType, value);
    }

    public MetatagMigrationItem(MetatagSchemaDiffOp diff)
    {
        m_include = true;
        DiffOp = diff;
        switch (diff.Action)
        {
            case MetatagSchemaDiffOp.ActionType.Delete:
                m_opType = "delete";
                break;
            case MetatagSchemaDiffOp.ActionType.Insert:
                m_opType = "insert";
                break;
            case MetatagSchemaDiffOp.ActionType.Update:
                m_opType = "update";
                break;
            default:
                m_opType = "UNKNOWN";
                break;
        }
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
