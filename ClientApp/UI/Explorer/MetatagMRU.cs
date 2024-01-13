using System.Collections.Generic;
using Thetacat.Metatags.Model;

namespace Thetacat.UI.Explorer;

public class MetatagMRU
{
    private static int maxSize = 10;

    private List<Metatag> m_recentTags = new();
    private int m_vectorClock = 0;

    public int VectorClock => m_vectorClock;

    public IEnumerable<Metatag> RecentTags => m_recentTags;

    public void TouchMetatag(Metatag metatag)
    {
        // see if its already in the list
        foreach (Metatag tag in m_recentTags)
        {
            if (tag.ID == metatag.ID)
                return;

            // otherwise, add this to the top
            m_recentTags.Insert(0, tag);
            if (m_recentTags.Count > maxSize)
                m_recentTags.RemoveRange(10, m_recentTags.Count - 9);

            m_vectorClock++;
        }
    }
}
