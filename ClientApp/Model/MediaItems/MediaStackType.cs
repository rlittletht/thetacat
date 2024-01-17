using System;

namespace Thetacat.Model;

public class MediaStackType
{
    public static readonly MediaStackType Version = new MediaStackType(s_VersionType);
    public static readonly MediaStackType Media = new MediaStackType(s_MediaType);

    public const int s_VersionType = 0;
    public const int s_MediaType = 1;

    // NOTE: If you add a type here, you have to adjust MediaItem.Stacks[] to include an additional null
    private static readonly string[] s_stackTypes =
    {
        "version",
        "media"
    };

    private readonly int m_type;

    public MediaStackType(int versionType)
    {
        m_type = versionType;
    }

    public MediaStackType(string type)
    {
        int i = 0;
        foreach (string s in s_stackTypes)
        {
            if (s == type)
            {
                m_type = i;
                return;
            }

            i++;
        }

        throw new ArgumentException($"{type} is not a valid MediaStackType");
    }

    public MediaStackType CreateFromString(string type)
    {
        return new MediaStackType(type);
    }

    public override string ToString() => s_stackTypes[m_type];

    public override int GetHashCode() => m_type;
    public override bool Equals(object? obj) => Equals(obj as MediaStackType);
    public bool Equals(MediaStackType? other) => other != null && m_type == other.m_type;

    public static implicit operator int(MediaStackType mediaStackType) => mediaStackType.m_type;
    public static implicit operator string(MediaStackType mediaStackType) => mediaStackType.ToString();
    public static implicit operator MediaStackType(string value) => new MediaStackType(value);
}
