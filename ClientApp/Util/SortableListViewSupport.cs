using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Documents;
using Thetacat.Controls;

namespace Thetacat.Util;

public class SortableListViewSupport
{
    private ListView m_listView;
    private GridViewColumnHeader? sortCol = null;
    private SortAdorner? sortAdorner;

    public void Sort(GridViewColumnHeader? column)
    {
        if (column == null)
            return;

        string sortBy = column.Tag?.ToString() ?? string.Empty;

        if (sortAdorner != null && sortCol != null)
        {
            AdornerLayer.GetAdornerLayer(sortCol)?.Remove(sortAdorner);
            m_listView.Items.SortDescriptions.Clear();
        }

        ListSortDirection newDir = ListSortDirection.Ascending;
        if (sortCol == column && sortAdorner?.Direction == newDir)
            newDir = ListSortDirection.Descending;

        sortCol = column;
        sortAdorner = new SortAdorner(sortCol, newDir);
        AdornerLayer.GetAdornerLayer(sortCol)?.Add(sortAdorner);
        m_listView.Items.SortDescriptions.Add(new SortDescription(sortBy, newDir));
    }

    public SortableListViewSupport(ListView listView)
    {
        m_listView = listView;
    }
}
