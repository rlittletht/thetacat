using System;
using Thetacat.Model;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;

namespace Thetacat.Migration.Elements.Metadata.UI;

public class MetatagPair
{
    public Metatag Metatag { get; init; }
    public string PseId { get; init; }

    public MetatagPair(Metatag metatag, string pseId)
    {
        Metatag = metatag;
        PseId = pseId;
    }

    public static bool operator ==(MetatagPair? left, MetatagPair? right)
    {
        if (object.ReferenceEquals(left, null) && object.ReferenceEquals(right, null)) return true;
        if (object.ReferenceEquals(left, null) || object.ReferenceEquals(right, null)) return false;

        if (left.Metatag != right.Metatag) return false;
        if (left.PseId != right.PseId) return false;

        return true;
    }

    public static bool operator !=(MetatagPair? left, MetatagPair? right)
    {
        return !(left == right);
    }

    public override bool Equals(object? obj)
    {
        MetatagPair? right = obj as MetatagPair;

        if (obj == null)
            throw new ArgumentException(nameof(obj));

        return this == right;
    }

    public override int GetHashCode() => $"{Metatag.ID}".GetHashCode();

    public override string ToString() => $"{Metatag}(${PseId})";
}
