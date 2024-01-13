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

    public static List<T> GetCheckedItems(TreeView view, AdditionalValidationDelegate? additionaValidation = null)
    {
        // build the list to check (only the marked items)
        List<T> checkedItems = new List<T>();

        foreach (T t in view.ItemsSource)
        {
            AddCheckedItemsToList(checkedItems, t, additionaValidation);
        }

        return checkedItems;
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
