using Thetacat.Migration.Elements.Media;
using Thetacat.Standards;

namespace Thetacat.Migration.Elements.Metadata;

public class SchemaMapping<T>
{
    public delegate void SetMediaItemDelegate<T1>(IMediaItem mediaItem, T1 t);
    public StandardsMappings.Builtin StandardId { get; init; }
    public int ItemTag { get; init; }
    public string Name { get; init; }
    public string Description { get; init; }

    private SetMediaItemDelegate<T>? SetMediaItem;

    public SchemaMapping(StandardsMappings.Builtin standardId, int itemTag, string name, string description, SetMediaItemDelegate<T>? setMediaItem)
    {
        StandardId = standardId;
        ItemTag = itemTag;
        Name = name;
        Description = description;
        SetMediaItem = setMediaItem;
    }

    public static SchemaMapping<T> CreateStandard(StandardsMappings.Builtin standardId, int itemTag)
    {
        return new SchemaMapping<T>(standardId, itemTag, string.Empty, string.Empty, null);
    }

    public static SchemaMapping<T> CreateUser(string name, string description)
    {
        return new SchemaMapping<T>(StandardsMappings.Builtin.User, 0, name, description, null);
    }

    public static SchemaMapping<T> CreateBuiltIn(SetMediaItemDelegate<T> setMediaItem)
    {
        return new SchemaMapping<T>(StandardsMappings.Builtin.Unknown, 0, string.Empty, string.Empty, setMediaItem);
    }
}
