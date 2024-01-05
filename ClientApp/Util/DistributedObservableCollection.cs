using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.PerformanceData;
using System.Security.RightsManagement;
using Thetacat.UI;

namespace Thetacat.Util;

public interface IObservableSegmentableCollectionHolder<T>
{
    public ObservableCollection<T> Items { get; }
    public bool EndSegmentAfter { get; set; }
}

// this is an observable collection or observable collections
// This collection is *segmentable*, which means any given line
// could be considered the "end of a segment", which means that the last
// item in the line is the end of the segment, even if it doesn't fill out
// the entire line.
// (This also means that if we reflow the items, that item has to continue
// to be considered the 'end of the segment')
public class DistributedObservableCollection<T, T1> 
    where T : class, IObservableSegmentableCollectionHolder<T1>
{
    public delegate T LineFactoryDelegate(T? reference);
    public delegate void MoveLinePropertiesDelegate(T from, T to);

    private readonly ObservableCollection<T> m_collection = new();

    public ObservableCollection<T> TopCollection => m_collection;

    private int m_itemsPerLine = 0;
    private readonly LineFactoryDelegate m_lineFactory;
    private readonly MoveLinePropertiesDelegate m_moveLineProperties;

    public DistributedObservableCollection(LineFactoryDelegate lineFactory, MoveLinePropertiesDelegate moveLinePropertiesDelegate)
    {
        m_lineFactory = lineFactory;
        m_moveLineProperties = moveLinePropertiesDelegate;
    }

    public void UpdateItemsPerLine(int newItemsPerLine)
    {
        int originalItemsPerLine = m_itemsPerLine;
        m_itemsPerLine = newItemsPerLine;

        AdjustItemsPerLine(originalItemsPerLine);
    }

    class SegmentInfo
    {
        public int Count { get; set; }
        public int StartLine { get; set; }
        public int PanelsPerLine { get; set; }

        public SegmentInfo(int count, int startLine, int panelsPerLine)
        {
            Count = count;
            StartLine = startLine;
            PanelsPerLine = panelsPerLine;
        }
    }

    private List<SegmentInfo>? m_segments;

    void BuildSegments(int panelsPerLine)
    {
        int count = 0, lineStart = 0, line = 0;

        m_segments = new List<SegmentInfo>();

        foreach (T item in m_collection)
        {
            count += item.Items.Count;
            if (item.EndSegmentAfter)
            {
                m_segments.Add(new SegmentInfo(count, lineStart, panelsPerLine));
                lineStart = line + 1;
                count = 0;
            }

            line++;
        }

        if (count > 0)
            m_segments.Add(new SegmentInfo(count, lineStart, panelsPerLine));
    }

    public int SegmentCount => m_segments?.Count ?? 0;

    T1 GetSegmentItemFromCollection(int iSegment, int iItem)
    {
        Debug.Assert(m_segments != null, nameof(m_segments) + " != null");
        SegmentInfo segment = m_segments[iSegment];
        int offsetIntoLine = iItem % segment.PanelsPerLine;

        return GetSegmentLineFromCollection(iSegment, iItem).Items[offsetIntoLine];
    }

    T GetSegmentLineFromCollection(int iSegment, int iItem)
    {
        Debug.Assert(m_segments != null, nameof(m_segments) + " != null");
        SegmentInfo segment = m_segments[iSegment];
        int lineSegmentFirst = segment.StartLine;
        int lineForItem = lineSegmentFirst + (iItem / segment.PanelsPerLine);

        return m_collection[lineForItem];
    }

    void MoveIObservableCollectionHolderItems(T from, T to)
    {
        // We only have EndSegmentAfter right now, and that is always specifically set, so nothing to move here.
    }

    /*----------------------------------------------------------------------------
        %%Function: ShiftlinesForGrow
        %%Qualified: Thetacat.Util.DistributedObservableCollection<T, T1>.ShiftlinesForGrow

        Unline shrink, we will now be pulling items from later lines because
        we are longer. This is easier and we can do it in one pass
    ----------------------------------------------------------------------------*/
    void ShiftLinesForGrow()
    {
        Debug.Assert(m_segments != null, nameof(m_segments) + " != null");

        List<T1> adding = new();

        int segmentItemCurrent = 0;
        int lineCurrent = 0;
        int segmentCurrent = 0;
        SegmentInfo segment = m_segments[segmentCurrent];

        while (true)
        {
            T line = m_collection[lineCurrent];

            adding.Clear();
            int pullFirst = segmentItemCurrent;
            int pullLast = Math.Min(segment.Count, segmentItemCurrent + m_itemsPerLine) - 1;

            for (int i = pullFirst; i <= pullLast; i++)
            {
                adding.Add(GetSegmentItemFromCollection(segmentCurrent, i));
            }

            // now clear the current line and add our items
            line.Items.Clear();
            foreach (T1 item in adding)
            {
                line.Items.Add(item);
            }

            T lineFrom = GetSegmentLineFromCollection(segmentCurrent, pullFirst);
            if (segmentItemCurrent == 0)
            {
                MoveIObservableCollectionHolderItems(lineFrom, line);
                m_moveLineProperties(lineFrom, line);
            }

            line.EndSegmentAfter = false;

            lineCurrent++;
            segmentItemCurrent = pullLast + 1;

            if (segmentItemCurrent == segment.Count)
            {
                line.EndSegmentAfter = true;
                // we're done with this segment. move to the next
                if (++segmentCurrent == m_segments.Count)
                    break;

                segmentItemCurrent = 0;
                segment = m_segments[segmentCurrent];
            }
        }

        // at this point we have some number of lines have moved from
        int linesToDelete = m_collection.Count - lineCurrent;
        while (linesToDelete-- > 0)
        {
            m_collection.RemoveAt(m_collection.Count - 1);
        }

        // lastly, rebuild the segments
        BuildSegments(m_itemsPerLine);
    }

    void ShiftLinesForShrink()
    {
        // we have to shrink lines, which means we (may) need to create new
        // lines

        // prescan the segments to determine the new line count
        int linesNeeded = 0;
        Debug.Assert(m_segments != null, nameof(m_segments) + " != null");
        foreach (SegmentInfo segment in m_segments)
        {
            linesNeeded += (segment.Count / m_itemsPerLine) + ((segment.Count % m_itemsPerLine) != 0 ? 1 : 0);
        }

        linesNeeded -= m_collection.Count;

        if (linesNeeded < 0)
            throw new Exception($"can't shrink the collection when shrinking lines");

        while (linesNeeded-- > 0)
        {
            m_collection.Add(m_lineFactory(null));
        }

        int lineReflowCurrent = m_collection.Count - 1;
        int iSegmentCurrent = m_segments.Count - 1;
        // at this point we have all the lines we need. now reflow starting at the end
        int segmentRemaining = m_segments[iSegmentCurrent].Count;
        List<T1> replacementItems = new();

        while (lineReflowCurrent >= 0)
        {
            bool firstItemInSegment = (segmentRemaining - m_itemsPerLine <= 0);
            int firstItem =
                (segmentRemaining % m_itemsPerLine) != 0
                    ? segmentRemaining - (segmentRemaining % m_itemsPerLine)
                    : segmentRemaining - m_itemsPerLine;

            int lastItem = segmentRemaining - 1;

            if (firstItem > lastItem)
                throw new Exception($"{firstItem}>{lastItem}?!?");

            // only move line properties when the first item of the segment is moving
            T lineFrom = GetSegmentLineFromCollection(iSegmentCurrent, lastItem);
            T lineTo = m_collection[lineReflowCurrent];

            if (firstItemInSegment)
            {
                MoveIObservableCollectionHolderItems(lineFrom, lineTo);
                m_moveLineProperties(lineFrom, lineTo);
            }

            lineTo.EndSegmentAfter = m_segments[iSegmentCurrent].Count == segmentRemaining;

                // we have to collect these items in a separate list because we might be modifying the same
                // line we are coming from...
                replacementItems.Clear();
            int i = 0;
            while (firstItem + i <= lastItem)
            {
                replacementItems.Add(GetSegmentItemFromCollection(iSegmentCurrent, firstItem + i));
                i++;
            }

            m_collection[lineReflowCurrent].Items.Clear();
            foreach (T1 item in replacementItems)
            {
                m_collection[lineReflowCurrent].Items.Add(item);
            }

            lineReflowCurrent--;
            segmentRemaining = firstItem;
            if (segmentRemaining == 0)
            {
                if (iSegmentCurrent == 0)
                {
                    if (lineReflowCurrent != -1)
                        throw new Exception($"{lineReflowCurrent} should be -1 when iSegmentCurrent is 0");
                    break;
                }

                iSegmentCurrent--;
                segmentRemaining = m_segments[iSegmentCurrent].Count;
            }
        }

        BuildSegments(m_itemsPerLine);
    }

    /*----------------------------------------------------------------------------
        %%Function: AdjustItemsPerLine
        %%Qualified: Thetacat.Util.DistributedObservableCollection<T, T1>.AdjustItemsPerLine

        This recalculates the items per line in the collecion.

        Since this collection is segmented, we can't just assume that every
        line has the same number of items. We will have to scan the collection
        to determine the real count as well as the real number of lines we are
        going to need
    ----------------------------------------------------------------------------*/
    public void AdjustItemsPerLine(int panelsPerLineOriginal)
    {
        if (m_collection.Count == 0 || panelsPerLineOriginal == m_itemsPerLine)
            return;

        if (m_itemsPerLine == 0)
            throw new Exception("items per line was 0 but we already had items?!");

        if (m_segments == null)
            BuildSegments(panelsPerLineOriginal);

        if (m_segments == null)
            throw new Exception("failed to build segments");

        if (panelsPerLineOriginal > m_itemsPerLine)
        {
            ShiftLinesForShrink();
        }
        else
        {
            ShiftLinesForGrow();
        }
    }

    public void AddSegment(IEnumerable<T1>? items = null)
    {
        // if there is already a segment, mark it as ending
        if (m_collection.Count > 0)
            m_collection[m_collection.Count - 1].EndSegmentAfter = true;

        if (items == null)
            return;

        foreach (T1 item in items)
        {
            AddItem(item);
        }

        m_collection[m_collection.Count - 1].EndSegmentAfter = true;
    }

    public T GetCurrentLine()
    {
        return m_collection[m_collection.Count - 1];
    }

    public void Clear()
    {
        m_collection.Clear();
        m_segments = null;
    }

    public void AddItem(T1 itemToAdd)
    {
        if (m_itemsPerLine == 0)
            throw new Exception("can't add an item before setting item count per line");

        bool startNewSegment =
            m_collection.Count > 0 && m_collection[m_collection.Count - 1].EndSegmentAfter;

        // figure out where to add this item
        if (m_collection.Count == 0
            || m_collection[m_collection.Count - 1].Items.Count == m_itemsPerLine
            || startNewSegment)
        {
            T newLine = m_lineFactory(null);
            
            m_collection.Add(newLine);
        }

        m_segments ??= new List<SegmentInfo>();

        if (m_segments.Count == 0 || startNewSegment)
        {
            m_segments.Add(new SegmentInfo(0, m_collection.Count - 1, m_itemsPerLine));
        }

        m_segments[m_segments.Count - 1].Count++;
        m_collection[m_collection.Count - 1].Items.Add(itemToAdd);
    }
}
