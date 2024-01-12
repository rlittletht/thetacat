using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Thetacat.Migration.Elements.Media;
using Thetacat.Migration.Elements.Metadata.UI;

namespace Thetacat.Util;

public class CheckableTreeViewSupport<T> where T: class, ICheckableTreeViewItem<T>
{
    public delegate bool AdditionalValidationDelegate(T t);

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

    public static void CheckItemSubtree()
    {
    }

    public static void ToggleItems(IEnumerable<object?>? items)
    {
        if (items == null)
            return;

        foreach (T? item in items)
        {
            if (item != null)
                item.Checked = !item.Checked;
        }
    }
}
