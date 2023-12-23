using Thetacat.Migration.Elements.Media;
using Thetacat.Migration.Elements.Metadata.UI.Media;
using Thetacat.Standards;

namespace Thetacat.Migration.Elements.Metadata.UI;

public class SchemaMapping<T>
{
    public delegate void SetMediaItemDelegate<T1>(IPseMediaItem pseMediaItem, T1 t);
    public MetatagStandards.Standard StandardId { get; init; }
    public int ItemTag { get; init; }
    public string Name { get; init; }
    public string Description { get; init; }

    private SetMediaItemDelegate<T>? SetMediaItem;

    public SchemaMapping(MetatagStandards.Standard standardId, int itemTag, string name, string description, SetMediaItemDelegate<T>? setMediaItem)
    {
        StandardId = standardId;
        ItemTag = itemTag;
        Name = name;
        Description = description;
        SetMediaItem = setMediaItem;
    }

    public static SchemaMapping<T> CreateStandard(MetatagStandards.Standard standardId, int itemTag)
    {
        return new SchemaMapping<T>(standardId, itemTag, string.Empty, string.Empty, null);
    }

    public static SchemaMapping<T> CreateUser(string name, string description)
    {
        return new SchemaMapping<T>(MetatagStandards.Standard.User, 0, name, description, null);
    }

    public static SchemaMapping<T> CreateBuiltIn(SetMediaItemDelegate<T> setMediaItem)
    {
        return new SchemaMapping<T>(MetatagStandards.Standard.Unknown, 0, string.Empty, string.Empty, setMediaItem);
    }

    public void SetMediaItemBuiltins(IPseMediaItem item, T t) => SetMediaItem?.Invoke(item, t);
}
