using System;
using System.Collections.Generic;
using Thetacat.Types;

namespace Thetacat.Metatags;

public class MetatagTreeItemMatcher : IMetatagMatcher<IMetatagTreeItem>
{
    private string? m_name;
    private string? m_id;
    private HashSet<string>? m_idSet;

    public bool IsMatch(IMetatagTreeItem item)
    {
        if (m_idSet != null && !m_idSet.Contains(item.ID))
            return false;

        if (m_name != null && string.Compare(m_name, item.Name, StringComparison.CurrentCultureIgnoreCase) != 0)
            return false;

        if (m_id != null && string.Compare(item.ID, m_id, StringComparison.InvariantCultureIgnoreCase) != 0)
            return false;

        return true;
    }

    public static MetatagTreeItemMatcher CreateIdSetMatch(IEnumerable<IMetatag> metatags)
    {
        MetatagTreeItemMatcher matcher = new();

        matcher.m_idSet = new HashSet<string>();
        foreach (IMetatag metatag in metatags)
        {
            matcher.m_idSet.Add(metatag.ID.ToString());
        }

        return matcher;
    }

    public static MetatagTreeItemMatcher CreateIdSetMatch(IEnumerable<string> idsToMatch)
    {
        MetatagTreeItemMatcher matcher = new();

        matcher.m_idSet = new HashSet<string>();
        foreach (string id in idsToMatch)
        {
            matcher.m_idSet.Add(id);
        }

        return matcher;
    }

    public static MetatagTreeItemMatcher CreateNameMatch(string name)
    {
        return
            new MetatagTreeItemMatcher()
            {
                m_name = name
            };
    }

    public static MetatagTreeItemMatcher CreateIdMatch(string id)
    {
        return
            new MetatagTreeItemMatcher()
            {
                m_id = id
            };
    }
}
