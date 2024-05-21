using System;
using System.Collections.Generic;
using System.Windows;
using Thetacat.Metatags.Model;
using Thetacat.Types;

namespace Thetacat.Explorer;

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
        }

        // otherwise, add this to the top
        m_recentTags.Insert(0, metatag);
        if (m_recentTags.Count > maxSize)
            m_recentTags.RemoveRange(10, m_recentTags.Count - 10);

        m_vectorClock++;
    }

    public void Set(IEnumerable<string> mru)
    {
        m_recentTags.Clear();
        foreach (string id in mru)
        {
            Metatag? tag = App.State.MetatagSchema.GetMetatagFromId(Guid.Parse(id));

            if (tag == null)
            {
                MessageBox.Show($"unknown metatag {id} in MRU");
                continue;
            }

            m_recentTags.Add(tag);
        }
    }
}
