using System;
using System.Collections.Generic;

namespace Thetacat.Types;

public class TimelineType
{
    public static readonly TimelineType MediaDate = new TimelineType(s_MediaDateType);
    public static readonly TimelineType ImportDate = new TimelineType(s_ImportDateType);
    public static readonly TimelineType None = new TimelineType(s_NoneType);

    public const int s_NoneType = 0;
    public const int s_MediaDateType = 1;
    public const int s_ImportDateType = 2;

    // NOTE: If you add a type here, you have to adjust ImportDateItem.Stacks[] to include an additional null
    private static readonly string[] s_timelineTypes =
    {
        "none",
        "media-date",
        "import-date"
    };

    private readonly int m_type;

    public TimelineType(int versionType)
    {
        m_type = versionType;
    }

    public TimelineType(string type)
    {
        int i = 0;
        foreach (string s in s_timelineTypes)
        {
            if (s == type)
            {
                m_type = i;
                return;
            }

            i++;
        }

        throw new ArgumentException($"{type} is not a valid TimelineType");
    }

    public TimelineType CreateFromString(string type)
    {
        return new TimelineType(type);
    }

    public override string ToString() => s_timelineTypes[m_type];

    public override int GetHashCode() => m_type;
    public override bool Equals(object? obj) => Equals(obj as TimelineType);
    public bool Equals(TimelineType? other) => other != null && m_type == other.m_type;

    public static implicit operator int(TimelineType mediaStackType) => mediaStackType.m_type;
    public static implicit operator string(TimelineType mediaStackType) => mediaStackType.ToString();
    public static implicit operator TimelineType(string value) => new TimelineType(value);
}

