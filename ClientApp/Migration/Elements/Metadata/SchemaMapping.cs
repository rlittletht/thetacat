namespace Thetacat.Migration.Elements.Metadata;

public class SchemaMapping<T>
{
    public delegate void SetMediaItemDelegate<T1>(IMediaItem mediaItem, T1 t);
    public string StandardTag { get; init; }
    public int ItemTag { get; init; }
    public string Name { get; init; }
    public string Description { get; init; }

    private SetMediaItemDelegate<T>? SetMediaItem;

    public SchemaMapping(string standardTag, int itemTag, string name, string description, SetMediaItemDelegate<T>? setMediaItem)
    {
        StandardTag = standardTag;
        ItemTag = itemTag;
        Name = name;
        Description = description;
        SetMediaItem = setMediaItem;
    }

    public static SchemaMapping<T> CreateStandard(string standardTag, int itemTag)
    {
        return new SchemaMapping<T>(standardTag, itemTag, string.Empty, string.Empty, null);
    }

    public static SchemaMapping<T> CreateUser(string name, string description)
    {
        return new SchemaMapping<T>("user", 0, name, description, null);
    }

    public static SchemaMapping<T> CreateBuiltIn(SetMediaItemDelegate<T> setMediaItem)
    {
        return new SchemaMapping<T>(string.Empty, 0, string.Empty, string.Empty, setMediaItem);
    }
}
