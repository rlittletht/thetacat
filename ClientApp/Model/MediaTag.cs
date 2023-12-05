using System;
using System.Windows;
using Thetacat.Metatags;

namespace Thetacat.Model;

public class MediaTag
{
    public Metatag Metatag { get; init; }
    public string? Value { get; init; }

    public MediaTag(Metatag metatag, string? value)
    {
        Metatag = metatag;
        Value = value;
    }

    public static MediaTag CreateMediaTag(MetatagSchema schema, Guid metatagId, string? value)
    {
        Model.Metatag? tag = schema.FindFirstMatchingItem(MetatagMatcher.CreateIdMatch(metatagId));

        if (tag == null)
        {
            MessageBox.Show($"MediaTag specified metatag ${metatagId} which did not exist in the schema. Creating a LocalOnly metatag");

            tag = new Metatag()
                  {
                      Description = $"Unknown metatag ${metatagId}",
                      ID = metatagId,
                      LocalOnly = true,
                      Name = $"__${metatagId}",
                      Parent = null,
                      Standard = "unknown"
                  };
        }

        return new MediaTag(tag, value);
    }
}
