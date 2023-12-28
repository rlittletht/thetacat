using System.Collections.Generic;
using System;
using System.Collections;

namespace Thetacat.Model;

public class MediaStackEnumerator : IEnumerator<MediaStack>
{
    public delegate bool FilterItemDelegate(MediaStack item);

    private FilterItemDelegate m_filter;
    private IEnumerator<KeyValuePair<Guid, MediaStack>> m_enumerator;

    public IEnumerator<MediaStack> GetEnumerator() => this;

    public MediaStackEnumerator(IEnumerable<KeyValuePair<Guid, MediaStack>> items, FilterItemDelegate filter)
    {
        m_enumerator = items.GetEnumerator();
        m_filter = filter;
    }

    public void Reset()
    {
        m_enumerator.Reset();
    }

    MediaStack IEnumerator<MediaStack>.Current => m_enumerator.Current.Value;
    object IEnumerator.Current => m_enumerator.Current.Value;

    public bool MoveNext()
    {
        while (m_enumerator.MoveNext())
        {
            if (m_filter(m_enumerator.Current.Value))
                return true;
        }

        return false;
    }

    public void Dispose()
    {
        m_enumerator.Dispose();
    }
}