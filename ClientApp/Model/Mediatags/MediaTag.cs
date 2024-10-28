using System;
using System.Windows;
using Thetacat.Metatags.Model;

namespace Thetacat.Model.Mediatags;

public class MediaTag
{
    public Metatag Metatag { get; init; }
    public string? Value { get; set; }

    // Deleted tags don't really exist (and will hide from being queried)
    // HOWEVER, when you try to add on top of a Deleted item, it will get resurrected

    // If we didn't do this, then when we go to create a new item that was previously
    // deleted, we will get a primary key violation (since the item really IS in the
    // database). So we persist the deleted item so long as it has a row in the database.

    // FUTURE: since we have all the deleted items, we know how many there are, which means
    // we know when we should call RemoveDeletedMediatagsAndResetTagClock
    public bool Deleted { get; set; }

    public MediaTag(Metatag metatag, string? value, bool deleted = false)
    {
        Metatag = metatag;
        Value = value;
        Deleted = deleted;
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
        if (other.Deleted != Deleted) return false;

        return true;
    }
}
