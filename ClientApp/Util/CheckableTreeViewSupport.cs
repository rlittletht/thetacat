using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Thetacat.Import.UI;
using Thetacat.Migration.Elements.Media;
using Thetacat.Migration.Elements.Metadata.UI;

namespace Thetacat.Util;

public class CheckableTreeViewSupport<T> where T: class, ICheckableTreeViewItem<T>
{
    public delegate bool AdditionalValidationDelegate(T t);
    public delegate bool FilterItemDelegate(T t);

    static void AddCheckedItemsToList(List<T> checkedItems, T t, AdditionalValidationDelegate? additionaValidation = null)
    {
        if (t.Checked && (additionaValidation == null || additionaValidation(t)))
        {
            checkedItems.Add(t);
            foreach (T child in t.Children)
            {
                AddCheckedItemsToList(checkedItems, child, additionaValidation);
            }
        }
    }

    public static List<T> GetCheckedItems(IEnumerable<T> root, AdditionalValidationDelegate? additionaValidation = null)
    {
        // build the list to check (only the marked items)
        List<T> checkedItems = new List<T>();

        foreach (T t in root)
        {
            AddCheckedItemsToList(checkedItems, t, additionaValidation);
        }

        return checkedItems;
    }

    public static List<T> GetCheckedItems(TreeView view, AdditionalValidationDelegate? additionaValidation = null)
    {
        return GetCheckedItems((IEnumerable<T>)view.ItemsSource);
    }

    public static void ToggleItems(IEnumerable<object?>? items, bool? set = null)
    {
        if (items == null)
            return;

        foreach (T? item in items)
        {
            if (item != null)
            {
                if (set != null)
                    item.Checked = set.Value;
                else
                    item.Checked = !item.Checked;

                ToggleItems(item.Children, set);
            }
        }
    }

    public static void DoCheckboxClickSetUnsetChildren(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox checkbox)
        {
            if (checkbox.DataContext is T node)
                ToggleItems(node.Children, node.Checked);
        }
    }

    public static bool? SetParentCheckStateForChildren(IEnumerable<T> subtree)
    {
        bool? fCurrent = null;
        bool fFirst = true;

        foreach (T item in subtree)
        {
            if (item.Children.Count > 0)
            {
                bool? childrenSet = SetParentCheckStateForChildren(item.Children);

                if (childrenSet != null)
                    item.Checked = childrenSet.Value;

                if (fFirst)
                    fCurrent = item.Checked;
                fFirst = false;

                if (fCurrent != childrenSet)
                    fCurrent = null;
            }

            if (fFirst)
                fCurrent = item.Checked;

            if (fCurrent != item.Checked)
                fCurrent = null;
            fFirst = false;
        }

        return fCurrent;
    }


    public static void FilterAndToggleSetSubtree(IEnumerable<T> subtree, FilterItemDelegate filter, bool? set = null)
    {
        foreach (T item in subtree)
        {
            if (filter(item))
                item.Checked = set ?? !item.Checked;

            FilterAndToggleSetSubtree(item.Children, filter, set);
        }
    }

    public static void FilterAndSetTree(T t, FilterItemDelegate filter, bool? set = null)
    {
        if (filter(t))
            t.Checked = set ?? !t.Checked;

        FilterAndToggleSetSubtree(t.Children, filter, set);
    }
}
