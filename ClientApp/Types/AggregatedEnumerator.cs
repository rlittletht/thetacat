using System;
using System.Collections;
using System.Collections.Generic;

namespace Thetacat.Types;

public class AggregatedEnumerator<T>: IEnumerator<T>
{
    private readonly List<T>[] m_collections;
    private readonly int[] m_precedingCounts;
    private int m_index = -1;
    private int m_enumerating = 0;

    public AggregatedEnumerator(params List<T>[] args)
    {
        int i = 0;
        int precedingCount = 0;

        m_collections = new List<T>[args.Length];
        m_precedingCounts = new int[args.Length];

        foreach (List<T> arg in args)
        {
            m_precedingCounts[i] = precedingCount;
            m_collections[i++] = arg;
            precedingCount += arg.Count;
        }
    }

    private int localIndex(int i) => i - m_precedingCounts[m_enumerating];

    public T Current
    {
        get
        {
            if (m_index == -1)
                throw new InvalidOperationException();

            return m_collections[m_enumerating][localIndex(m_index)];
        }
    }

    object IEnumerator.Current => Current!;

    public bool MoveNext()
    {
        m_index++;
        
        // if we're at the bounds of this collection. go to the next
        // (and continue skipping until we find one with items in it)
        while (localIndex(m_index) >= m_collections[m_enumerating].Count)
        {
            if (m_enumerating == m_collections.Length - 1)
            {
                m_index = -1;
                return false;
            }

            m_enumerating++;
        }

        return true;
    }

    public void Reset()
    {
        m_index = -1;
        m_enumerating = 0;
    }

    public void Dispose()
    {
    }
}
