using System;
using System.Collections.Generic;

namespace Thetacat.Types;

public class TimelineOrder
{
    public static readonly TimelineOrder Ascending = new TimelineOrder(s_AscendingType);
    public static readonly TimelineOrder Descending = new TimelineOrder(s_DescendingType);
    public static readonly TimelineOrder None = new TimelineOrder(s_NoneType);

    public const int s_NoneType = 0;
    public const int s_AscendingType = 1;
    public const int s_DescendingType = 2;

    // NOTE: If you add a type here, you have to adjust DescendingItem.Stacks[] to include an additional null
    private static readonly string[] s_timelineTypes =
    {
        "none",
        "ascending",
        "descending"
    };

    private readonly int m_type;

    public TimelineOrder(int versionType)
    {
        m_type = versionType;
    }

    public TimelineOrder(string type)
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

        throw new ArgumentException($"{type} is not a valid TimelineOrder");
    }

    public TimelineOrder CreateFromString(string type)
    {
        return new TimelineOrder(type);
    }

    public override string ToString() => s_timelineTypes[m_type];

    public override int GetHashCode() => m_type;
    public override bool Equals(object? obj) => Equals(obj as TimelineOrder);
    public bool Equals(TimelineOrder? other) => other != null && m_type == other.m_type;

    public static implicit operator int(TimelineOrder mediaStackType) => mediaStackType.m_type;
    public static implicit operator string(TimelineOrder mediaStackType) => mediaStackType.ToString();
    public static implicit operator TimelineOrder(string value) => new TimelineOrder(value);
}

