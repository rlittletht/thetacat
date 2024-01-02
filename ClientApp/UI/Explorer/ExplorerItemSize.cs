using System;

namespace Thetacat.UI.Explorer;

public class ExplorerItemSize
{
    public static readonly ExplorerItemSize Medium = new ExplorerItemSize(s_Medium);
    public static readonly ExplorerItemSize Large = new ExplorerItemSize(s_Large);
    public static readonly ExplorerItemSize Small = new ExplorerItemSize(s_Small);

    public const int s_Medium = 0;
    public const int s_Large = 1;
    public const int s_Small = 2;

    private static readonly string[] s_itemSizes =
    {
        "medium",
        "large",
        "small"
    };

    private readonly int m_size;

    public ExplorerItemSize(int versionSize)
    {
        m_size = versionSize;
    }

    public ExplorerItemSize(string _size)
    {
        int i = 0;
        foreach (string s in s_itemSizes)
        {
            if (s == _size)
            {
                m_size = i;
                return;
            }

            i++;
        }

        throw new ArgumentException($"{_size} is not a valid MediaStackType");
    }

    public ExplorerItemSize CreateFromString(string _size)
    {
        return new ExplorerItemSize(_size);
    }

    public override string ToString() => s_itemSizes[m_size];

    public override int GetHashCode() => m_size;
    public override bool Equals(object? obj) => Equals(obj as ExplorerItemSize);
    public bool Equals(ExplorerItemSize? other) => other != null && m_size == other.m_size;

    public static implicit operator int(ExplorerItemSize itemSize) => itemSize.m_size;
    public static implicit operator string(ExplorerItemSize itemSize) => itemSize.ToString();
    public static implicit operator ExplorerItemSize(string value) => new ExplorerItemSize(value);
}
