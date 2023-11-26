using System;
using Thetacat.ServiceClient;

namespace Thetacat.Model;

public class Metatag
{
    public static Guid IdMatchAny = new Guid(0x09080706, 0x0001, 0x0002, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f, 0x10, 0x11);

    public Guid ID { get; init; } = Guid.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid? Parent { get; set; }

    public static Metatag CreateFromService(ServiceMetatag serviceMetatag)
    {
        return new Metatag()
               {
                   ID = serviceMetatag.ID, 
                   Parent = serviceMetatag.Parent, 
                   Name = serviceMetatag.Name ?? string.Empty,
                   Description = serviceMetatag.Description ?? string.Empty
               };
    }

    public static bool operator ==(Metatag? left, Metatag? right)
    {
        if (object.ReferenceEquals(left, null) && object.ReferenceEquals(right, null)) return true;
        if (object.ReferenceEquals(left, null) || object.ReferenceEquals(right, null)) return false;

        if (left.ID != right.ID && left.ID != IdMatchAny && right.ID != IdMatchAny) return false;
        if (left.Name != right.Name) return false;
        if (left.Description != right.Description) return false;
        if (left.Parent != right.Parent && left.Parent != IdMatchAny && right.Parent != IdMatchAny) return false;

        return true;
    }

    public static bool operator !=(Metatag left, Metatag right)
    {
        return !(left == right);
    }

    public override bool Equals(object? obj)
    {
        Metatag? right = obj as Metatag;

        if (obj == null)
            throw new ArgumentException(nameof(obj));

        return this == right;
    }

    public override int GetHashCode() => $"{ID}".GetHashCode();

    public override string ToString() => $"{ID}: '{Name}({Description}) => {Parent}";
}


