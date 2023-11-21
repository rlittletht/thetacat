using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Thetacat.Migration.Elements;
using Thetacat.Types;

namespace Thetacat.Migration;

/// <summary>
/// Interaction logic for ElementsMigration.xaml
/// </summary>
public partial class ElementsMigration : Window
{
    private List<ElementsMediaItem>? m_items;
    readonly List<PathSubstitution> m_pathSubstitutions = new() { new PathSubstitution { From = "//ix", To = "//pix" } };

    void BuildMetadataReportFromDatabase(string database)
    {
        ElementsDb db = ElementsDb.Create(database);

        metaTagsListView.ItemsSource = db.ReadMetadataTags();
        m_items = db.ReadMediaItems();
        mediaItemsListView.ItemsSource = m_items;

        CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(mediaItemsListView.ItemsSource);
        view.Filter = FilterMediaItem;
        substDatagrid.ItemsSource = m_pathSubstitutions;

        db.Close();
    }

    public ElementsMigration(string database)
    {
        InitializeComponent();
        BuildMetadataReportFromDatabase(database);
    }

    private int m_countRunningVerifyTasks = 0;

    void SetVerifyResult()
    {
        TriState tri = TriState.Maybe;

        foreach (ElementsMediaItem item in m_items)
        {
            if (item.PathVerified == TriState.No)
                tri = TriState.No;

            if (item.PathVerified == TriState.Yes && tri != TriState.No)
                tri = TriState.Yes;

            if (item.PathVerified == TriState.Maybe && tri != TriState.No)
                tri = TriState.Maybe;
        }

        switch (tri)
        {
            case TriState.Maybe:
                VerifyResult.Text = "?";
                VerifyResult.Foreground = new SolidColorBrush(Colors.Black);
                break;
            case TriState.Yes:
                VerifyResult.Text = "+";
                VerifyResult.Foreground = new SolidColorBrush(Colors.Green);
                break;
            case TriState.No:
                VerifyResult.Text = "X";
                VerifyResult.Foreground = new SolidColorBrush(Colors.Red);
                break;
        }

        VerifyResult.Visibility = Visibility.Visible;
    }

    void CompleteVerifyTask()
    {
        if (Interlocked.Decrement(ref m_countRunningVerifyTasks) == 0)
        {
            VerifyStatus.Visibility = Visibility.Collapsed;
            ((Storyboard?)VerifyStatus.Resources.FindName("spinner"))?.Stop();
            SetVerifyResult();
        }
    }

    void VerifyPathSet(int start, int end, Dictionary<string, string> subs)
    {
        Interlocked.Increment(ref m_countRunningVerifyTasks);
        TaskScheduler uiScheduler = TaskScheduler.FromCurrentSynchronizationContext();

        Task.Run(
                () =>
                {
                    for (int i = start; i < end; i++)
                    {
                        m_items[i].CheckPath(subs);
                    }
                })
           .ContinueWith(delegate { CompleteVerifyTask(); }, uiScheduler);
    }

    private void VerifyPaths(object sender, RoutedEventArgs e)
    {
        Dictionary<string, string> pathSubst = new();

        foreach (PathSubstitution sub in m_pathSubstitutions)
        {
            pathSubst.Add(sub.From, sub.To);
        }

        ((Storyboard?)VerifyStatus.Resources.FindName("spinner"))?.Begin();

        VerifyResult.Visibility = Visibility.Hidden;
        VerifyStatus.Visibility = Visibility.Visible;

        // split the list into 4 parts and do them in parallel
        int segLength = m_items.Count / 10;
        int segStart = 0;
        for (int iSeg = 0; iSeg < 10; iSeg++)
        {
            int segEnd = Math.Min(segStart + segLength, m_items.Count);

            VerifyPathSet(segStart, segEnd, pathSubst);
            if (segEnd == m_items.Count)
                break;

            segStart += segLength;
        }

        if (segStart < m_items.Count)
            VerifyPathSet(segStart, m_items.Count, pathSubst);
    }

    bool FilterMediaItem(object o)
    {
        ElementsMediaItem item = (ElementsMediaItem)o;
        
        if (FilterItems.SelectedItem == null)
            return true;

        switch (((ComboBoxItem)FilterItems.SelectedItem).Content as string)
        {
            case "All":
                return true;
            case "Maybe":
                return (item.PathVerified == TriState.Maybe);
            case "Yes":
                return (item.PathVerified == TriState.Yes);
            case "No":
                return (item.PathVerified == TriState.No);
        }

        return true;
    }

    private void DoFilterItemChanged(object sender, SelectionChangedEventArgs e)
    {
        CollectionViewSource.GetDefaultView(mediaItemsListView.ItemsSource).Refresh();
    }
}
