using System;
using Thetacat.Types;

namespace Thetacat.Model.Metatags;

public class MetatagMatcher : IMetatagMatcher<IMetatag>
{
    private string? m_name;
    private Guid? m_id;

    public bool IsMatch(IMetatag item)
    {
        if (m_name != null && string.Compare(m_name, item.Name, StringComparison.CurrentCultureIgnoreCase) != 0)
            return false;

        if (m_id != null && m_id != item.ID)
            return false;

        return true;
    }

    public static MetatagMatcher CreateNameMatch(string name)
    {
        return
            new MetatagMatcher()
            {
                m_name = name
            };
    }

    public static MetatagMatcher CreateIdMatch(Guid id)
    {
        return
            new MetatagMatcher()
            {
                m_id = id
            };
    }

    public static MetatagMatcher CreateIdMatch(string id)
    {
        return
            new MetatagMatcher()
            {
                m_id = Guid.Parse(id)
            };
    }
}
