using System;
using Thetacat.Types;

namespace Thetacat.Metatags;

public class MetatagTreeItemMatcher : IMetatagMatcher<IMetatagTreeItem>
{
    private string? m_name;
    private string? m_id;

    public bool IsMatch(IMetatagTreeItem item)
    {
        if (m_name != null && string.Compare(m_name, item.Name, StringComparison.CurrentCultureIgnoreCase) != 0)
            return false;

        if (m_id != null && string.Compare(item.ID, m_id, StringComparison.InvariantCultureIgnoreCase) != 0)
            return false;

        return true;
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
