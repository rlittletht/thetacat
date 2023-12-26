using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Thetacat.Migration.Elements.Media;
using Thetacat.Migration.Elements.Metadata.UI;

namespace Thetacat.Util;

public class CheckableListViewSupport<T> where T: ICheckableListViewItem
{
    public delegate bool AdditionalValidationDelegate(T t);

    /*----------------------------------------------------------------------------
        %%Function: DoKeyDown
        %%Qualified: Thetacat.Migration.Elements.Metadata.UI.MediaMigration.DoKeyDown

        we might have to do something special here to prevent it from deselecting
        our selection when space is pressed
    ----------------------------------------------------------------------------*/
    public static void DoKeyDown(ListView listView, object sender, KeyEventArgs e)
    {
        if (!e.IsRepeat && e.Key == Key.Space)
        {
            bool notMixed = listView.SelectedItems.Cast<object>().Any(item => ((T)item).Checked)
                ^ listView.SelectedItems.Cast<object>().Any(item => !((T)item).Checked);

            foreach (object? item in listView.SelectedItems)
            {
                if (item is T pseItem)
                    pseItem.Checked = !notMixed || !pseItem.Checked;
            }
        }
    }

    public static List<T> GetCheckedItems(ListView listView, AdditionalValidationDelegate? additionaValidation = null)
    {
        // build the list to check (only the marked items)
        List<T> checkedItems = new List<T>();

        foreach (T t in listView.ItemsSource)
        {
            if (t.Checked && (additionaValidation == null || additionaValidation(t)))
                checkedItems.Add(t);
        }

        return checkedItems;
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

    public static void DoToggleSelected(ListView listView, object sender, RoutedEventArgs e)
    {
        ToggleItems(listView.SelectedItems as IEnumerable<object?>);
    }
}
