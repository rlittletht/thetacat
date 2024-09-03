using System;
using TCore.PostfixText;
using Thetacat.Metatags.Model;
using Thetacat.Model;

namespace Thetacat.Filtering;

public class Filter
{
    public FilterDefinition Definition;
    public FilterType FilterType { get; set; }
    public Guid Id { get; set; }
    
    public Filter(FilterDefinition definition, FilterType filterType, Guid? id)
    {
        FilterType = filterType;
        Id = id ?? Guid.Empty;
        Definition = definition;
    }

    public Filter(string name, string description, string expression)
    {
        FilterType = FilterType.Local;
        Id = Guid.Empty;
        Definition = new FilterDefinition(name, description, expression);
    }

    public bool EvaluateForMedia(MediaItem mediaItem)
    {
        FilterValueClient client = new FilterValueClient(mediaItem);

        return Definition.Expression.FEvaluate(client);
    }

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
}
