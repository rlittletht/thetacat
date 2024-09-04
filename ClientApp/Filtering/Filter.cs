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

    /*----------------------------------------------------------------------------
        %%Function: IsDefault
        %%Qualified: Thetacat.Filtering.Filter.IsDefault
    ----------------------------------------------------------------------------*/
    bool IsDefault()
    {
        string defaultFilter = App.State.ActiveProfile.DefaultFilterName ?? "";

        if (defaultFilter == Definition.FilterName || defaultFilter == Id.ToString())
            return true;

        return false;
    }

    public string DialogDisplayName => $"{Definition.FilterName}{(FilterType == FilterType.Workgroup ? "*" : "")}{(IsDefault() ? " [default]" : "")}";

    public string DisplayName => $"{Definition.FilterName}{(FilterType == FilterType.Workgroup ? "*" : "")}";
    public override string ToString() => DisplayName;

    // this isn't a perfect match (it guesses that Guid strings are workgroup Ids), but its pretty good
    public string LooseId => FilterType == FilterType.Local ? Definition.FilterName : Id.ToString();
    public bool MatchLooseId(string looseId) => string.Compare(LooseId, looseId, StringComparison.OrdinalIgnoreCase) == 0;
}
