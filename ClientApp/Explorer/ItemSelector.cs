using System.Collections.Generic;
using Thetacat.Model;

namespace Thetacat.Explorer;

public delegate void OnSelectionChangedDelegate(IEnumerable<MediaExplorerItem> selectedItems);

public class ItemSelector
{
    private MediaExplorerCollection? m_collection;
    readonly OnSelectionChangedDelegate m_onSelectionChanged;
    private LineItemOffset? m_pinnedSelectionClick;
    private bool m_pinnedSelectionClickSelect = false;
    private readonly HashSet<MediaExplorerItem> m_itemsSelected = new();
    private int m_itemsSelectedVectorClock = 0;

    public int VectorClock => m_itemsSelectedVectorClock;
    public IEnumerable<MediaExplorerItem> SelectedItems => m_itemsSelected;

    public ItemSelector(MediaExplorerCollection? collection, OnSelectionChangedDelegate onSelectionChanged)
    {
        m_collection = collection;
        m_onSelectionChanged = onSelectionChanged;
    }

    public void ResetCollection(MediaExplorerCollection? collection)
    {
        m_collection = collection;
    }


    private void ClearSelectedItems()
    {
        foreach (MediaExplorerItem item in m_itemsSelected)
        {
            item.Selected = false;
        }
        m_itemsSelected.Clear();
    }

    private void SelectExplorerItem(MediaExplorerItem item)
    {
        m_itemsSelected.Add(item);
        item.Selected = true;
    }

    private void UnselectExplorerItem(MediaExplorerItem item)
    {
        if (m_itemsSelected.Contains(item))
        {
            m_itemsSelected.Remove(item);
            item.Selected = false;
        }
    }

    /*----------------------------------------------------------------------------
        %%Function: ToggleSelectExplorerItem
        %%Qualified: Thetacat.UI.MediaExplorer.ToggleSelectExplorerItem

        Toggles the selection state of the given item.  Returns whether we
        selected or unselected the item.
    ----------------------------------------------------------------------------*/
    private bool ToggleSelectExplorerItem(MediaExplorerItem item)
    {
        if (m_itemsSelected.Contains(item))
        {
            m_itemsSelected.Remove(item);
            item.Selected = false;
            return false;
        }
        else
        {
            m_itemsSelected.Add(item);
            item.Selected = true;
            return true;
        }
    }

    void NotifySelectionChanged()
    {
        m_itemsSelectedVectorClock++;
        m_onSelectionChanged(m_itemsSelected);
    }

    /*----------------------------------------------------------------------------
        %%Function: _SelectPanel
        %%Qualified: Thetacat.UI.MediaExplorer._SelectPanel

        This is just a regular mouse click. Deselect everything else and set the
        pinned click
    ----------------------------------------------------------------------------*/
    public void _SelectPanel(MediaExplorerItem? context)
    {
        if (m_collection == null)
            return;

        m_collection.DebugVerifySelectedItems(m_itemsSelected);
        ClearSelectedItems();
        m_pinnedSelectionClick = null;
        if (context != null)
        {
            SelectExplorerItem(context);
            m_pinnedSelectionClick = m_collection.GetLineItemOffsetForMediaItem(context);
            m_pinnedSelectionClickSelect = true;
        }

        m_collection.DebugVerifySelectedItems(m_itemsSelected);
        NotifySelectionChanged();
    }

    /*----------------------------------------------------------------------------
        %%Function: _ExtendSelectPanel
        %%Qualified: Thetacat.UI.MediaExplorer._ExtendSelectPanel

        This is a shift+click. It extends from the pinned selection click to the
        current offset
    ----------------------------------------------------------------------------*/
    public void _ExtendSelectPanel(MediaExplorerItem? context)
    {
        if (m_collection == null)
            return;

        m_collection.DebugVerifySelectedItems(m_itemsSelected);
        if (context == null)
        {
            ClearSelectedItems();
            m_pinnedSelectionClick = null;
            m_pinnedSelectionClickSelect = false;
            m_collection.DebugVerifySelectedItems(m_itemsSelected);
            NotifySelectionChanged();
            return;
        }

        m_pinnedSelectionClick ??= new LineItemOffset(0, 0);
        LineItemOffset? thisItem = m_collection.GetLineItemOffsetForMediaItem(context);

        if (thisItem != null)
        {
            List<MediaExplorerItem> extendBy = m_collection.GetMediaItemsBetween(m_pinnedSelectionClick, thisItem);

            foreach (MediaExplorerItem extendByItem in extendBy)
            {
                SelectExplorerItem(extendByItem);
            }

            m_collection.DebugVerifySelectedItems(m_itemsSelected);
            NotifySelectionChanged();
        }
    }

    public void _ContextSelectPanel(MediaExplorerItem? context)
    {
        if (m_collection == null)
            return;

        if (context == null)
        {
            ClearSelectedItems();
            m_pinnedSelectionClick = null;
            m_pinnedSelectionClickSelect = false;
            m_collection.DebugVerifySelectedItems(m_itemsSelected);
            NotifySelectionChanged();
            return;
        }

        // if the current item is already selected, do nothing
        if (m_itemsSelected.Contains(context))
            return;

        _SelectPanel(context);
    }

    /*----------------------------------------------------------------------------
        %%Function: _AddSelectPanel
        %%Qualified: Thetacat.UI.MediaExplorer._AddSelectPanel

        This is a control click. It toggles the item in the current collection
    ----------------------------------------------------------------------------*/
    public void _AddSelectPanel(MediaExplorerItem? context)
    {
        if (m_collection == null)
            return;

        m_collection.DebugVerifySelectedItems(m_itemsSelected);

        if (context == null)
        {
            ClearSelectedItems();
            m_pinnedSelectionClick = null;
            m_pinnedSelectionClickSelect = false;
            m_collection.DebugVerifySelectedItems(m_itemsSelected);
            NotifySelectionChanged();
            return;
        }

        // remember whether the last control+click selected or deselected the item
        m_pinnedSelectionClickSelect = ToggleSelectExplorerItem(context);
        m_pinnedSelectionClick = m_collection.GetLineItemOffsetForMediaItem(context);
        m_collection.DebugVerifySelectedItems(m_itemsSelected);
        NotifySelectionChanged();
    }

    public void _StickyExtendSelectPanel(MediaExplorerItem? context)
    {
        if (m_collection == null)
            return;

        m_collection.DebugVerifySelectedItems(m_itemsSelected);
        if (context == null)
        {
            ClearSelectedItems();
            m_pinnedSelectionClick = null;
            m_pinnedSelectionClickSelect = false;
            m_collection.DebugVerifySelectedItems(m_itemsSelected);
            NotifySelectionChanged();
            return;
        }

        // if there's no pinned selection, then assume from start, selecting
        if (m_pinnedSelectionClick == null)
        {
            m_pinnedSelectionClick = new LineItemOffset(0, 0);
            m_pinnedSelectionClickSelect = true;
        }

        LineItemOffset? thisItem = m_collection.GetLineItemOffsetForMediaItem(context);
        if (thisItem != null)
        {
            List<MediaExplorerItem> extendBy = m_collection.GetMediaItemsBetween(m_pinnedSelectionClick, thisItem);

            foreach (MediaExplorerItem extendByItem in extendBy)
            {
                if (m_pinnedSelectionClickSelect)
                    SelectExplorerItem(extendByItem);
                else
                    UnselectExplorerItem(extendByItem);
            }

            NotifySelectionChanged();
            m_collection.DebugVerifySelectedItems(m_itemsSelected);
        }
    }

}
