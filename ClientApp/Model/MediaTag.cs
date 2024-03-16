using System;
using System.Windows;
using Thetacat.Metatags.Model;

namespace Thetacat.Model;

public class MediaTag
{
    public Metatag Metatag { get; init; }
    public string? Value { get; set; }

    public MediaTag(Metatag metatag, string? value)
    {
        Metatag = metatag;
        Value = value;
    }

    public static MediaTag CreateMediaTag(MetatagSchema schema, Guid metatagId, string? value)
    {
        Metatag? tag = schema.GetMetatagFromId(metatagId);

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

    public bool Equals(MediaTag other)
    {
        if (other.Value != Value) return false;
        if (other.Metatag.ID != Metatag.ID) return false;

        return true;
    }
}
