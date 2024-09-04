using System;
using TCore.PostfixText;
using Thetacat.Metatags.Model;
using Thetacat.Model;
using Thetacat.Model.Workgroups;

namespace Thetacat.Filtering;

public class Filter
{
    public FilterDefinition Definition { get; set; }
    public FilterType FilterType { get; set; }
    public Guid Id { get; set; }
    
    /*----------------------------------------------------------------------------
        %%Function: Filter
        %%Qualified: Thetacat.Filtering.Filter.Filter
    ----------------------------------------------------------------------------*/
    public Filter(FilterDefinition definition, FilterType filterType, Guid? id)
    {
        FilterType = filterType;
        Id = id ?? Guid.Empty;
        Definition = definition;
    }

    /*----------------------------------------------------------------------------
        %%Function: Filter
        %%Qualified: Thetacat.Filtering.Filter.Filter
    ----------------------------------------------------------------------------*/
    public Filter(WorkgroupFilter filter)
    {
        FilterType = FilterType.Workgroup;
        Id = filter.Id;
        Definition = new FilterDefinition(filter.Name, filter.Description, filter.Expression);
    }


    /*----------------------------------------------------------------------------
        %%Function: Filter
        %%Qualified: Thetacat.Filtering.Filter.Filter
    ----------------------------------------------------------------------------*/
    public Filter(string name, string description, string expression)
    {
        FilterType = FilterType.Local;
        Id = Guid.Empty;
        Definition = new FilterDefinition(name, description, expression);
    }

    /*----------------------------------------------------------------------------
        %%Function: EvaluateForMedia
        %%Qualified: Thetacat.Filtering.Filter.EvaluateForMedia
    ----------------------------------------------------------------------------*/
    public bool EvaluateForMedia(MediaItem mediaItem)
    {
        FilterValueClient client = new FilterValueClient(mediaItem);

        return Definition.Expression.FEvaluate(client);
    }

    /*----------------------------------------------------------------------------
        %%Function: GetDisplayString
        %%Qualified: Thetacat.Filtering.Filter.GetDisplayString
    ----------------------------------------------------------------------------*/
    public string GetDisplayString()
    {
        return Definition.Expression.ToString(
            (field) =>
            {
                Guid metatagId = Guid.Parse(field);

                Metatag? tag = App.State.MetatagSchema.GetMetatagFromId(metatagId);

                if (tag == null)
                    return "[UNKNOWN]";

                return $"[{tag.Name}]";
            });
    }

    /*----------------------------------------------------------------------------
        %%Function: AddClause
        %%Qualified: Thetacat.Filtering.Filter.AddClause
    ----------------------------------------------------------------------------*/
    public void AddClause(Guid metatagId, bool fSet, PostfixOperator.Op? postfixOp)
    {
        string val = fSet ? "$true" : $"false";

        Definition.Expression.AddExpression(Expression.Create(Value.CreateForField(metatagId.ToString("B")), Value.Create(val), new ComparisonOperator(ComparisonOperator.Op.Eq)));

        if (postfixOp != null)
        {
            // don't push a postfix op if we don't have enough values
            if (Definition.Expression.ValuesRemainingAfterReduce() >= 2)
                Definition.Expression.AddOperator(new PostfixOperator(postfixOp.Value));
        }
    }

    public string DisplayName => $"{(FilterType == FilterType.Workgroup ? "*" : "")}{Definition.FilterName}";
    public override string ToString() => DisplayName;
}
