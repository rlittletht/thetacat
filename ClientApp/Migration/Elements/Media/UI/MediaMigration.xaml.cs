using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Packaging;
using System.Linq;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Thetacat.Controls;
using Thetacat.Migration.Elements.Media;
using Thetacat.Migration.Elements.Media.UI;
using Thetacat.Migration.Elements.Metadata.UI.Media;
using Thetacat.Types;

namespace Thetacat.Migration.Elements.Metadata.UI;

/// <summary>
/// Interaction logic for MediaMigration.xaml
/// </summary>
public partial class MediaMigration : UserControl
{
    private IAppState? m_appState;
    private ElementsMigrate? m_migrate;

    public MediaMigration()
    {
        InitializeComponent();
    }

    private void VerifyPaths(object sender, RoutedEventArgs e)
    {
        VerifyPaths();
    }

    private void DoFilterItemChanged(object sender, SelectionChangedEventArgs e)
    {
        CollectionViewSource.GetDefaultView(mediaItemsListView.ItemsSource).Refresh();
    }

    public void Initialize(IAppState appState, ElementsDb db, ElementsMigrate migrate)
    {
        m_appState = appState;
        m_migrate = migrate;

        m_migrate.MediaMigrate.SetMediaItems(new List<PseMediaItem>(db.ReadMediaItems(m_migrate.MetatagMigrate)));

        mediaItemsListView.ItemsSource = m_migrate.MediaMigrate.MediaItems;

        CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(mediaItemsListView.ItemsSource);
        view.Filter = FilterMediaItem;

        SetSubstitutionsFromSettings();
    }

    bool FilterMediaItem(object o)
    {
        PseMediaItem item = (PseMediaItem)o;

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

    public void SetSubstitutionsFromSettings()
    {
        if (m_appState == null || m_migrate == null)
            throw new Exception("Not initialized");

        foreach (string s in m_appState.Settings.Settings.RgsValue("LastElementsSubstitutions"))
        {
            string[] pair = s.Split(",");
            if (pair.Length != 2)
            {
                MessageBox.Show($"bad subst setting in registry: {s}");
                continue;
            }

            m_migrate.MediaMigrate.PathSubstitutions.Add(new PathSubstitution { From = pair[0], To = pair[1] });
        }

        substDatagrid.ItemsSource = m_migrate.MediaMigrate.PathSubstitutions;
    }

    private int m_countRunningVerifyTasks = 0;

    void SetVerifyResult()
    {
        if (m_appState == null || m_migrate == null)
            throw new Exception("Not initialized");

        TriState tri = TriState.Maybe;

        foreach (PseMediaItem item in m_migrate.MediaMigrate.MediaItems)
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
        if (VerifyStatus == null)
            return;

        if (Interlocked.Decrement(ref m_countRunningVerifyTasks) == 0)
        {
            VerifyStatus.Visibility = Visibility.Collapsed;
            ((Storyboard?)VerifyStatus.Resources.FindName("spinner"))?.Stop();
            SetVerifyResult();
        }
    }

    void VerifyPathSet(int start, int end, Dictionary<string, string> subs)
    {
        if (m_migrate == null || m_appState == null)
            throw new Exception("not initialized");

        Interlocked.Increment(ref m_countRunningVerifyTasks);
        TaskScheduler uiScheduler = TaskScheduler.FromCurrentSynchronizationContext();

        Task.Run(
                () =>
                {
                    for (int i = start; i < end; i++)
                    {
                        m_migrate.MediaMigrate.MediaItems[i].CheckPath(m_appState, subs);
                    }
                })
           .ContinueWith(delegate { CompleteVerifyTask(); }, uiScheduler);
    }

    public void VerifyPaths()
    {
        if (m_appState == null || m_migrate == null)
            throw new Exception("Not initialized");

        Dictionary<string, string> pathSubst = new();

        List<string> regValues = new();

        foreach (PathSubstitution sub in m_migrate.MediaMigrate.PathSubstitutions)
        {
            pathSubst.Add(sub.From, sub.To);
            regValues.Add($"{sub.From},{sub.To}");
        }

        // persist the paths to the registry here
        m_appState.Settings.Settings.SetRgsValue("LastElementsSubstitutions", regValues.ToArray());
        m_appState.Settings.Settings.Save();

        ((Storyboard?)VerifyStatus.Resources.FindName("spinner"))?.Begin();

        VerifyResult.Visibility = Visibility.Hidden;
        VerifyStatus.Visibility = Visibility.Visible;

        // build the list to check (only the marked items)
        List<PseMediaItem> checkedItems = new List<PseMediaItem>();
        foreach (PseMediaItem item in m_migrate.MediaMigrate.MediaItems)
        {
            if (item.Migrate)
                checkedItems.Add(item);
        }

        // split the list into 4 parts and do them in parallel
        int segLength = checkedItems.Count; //  / 10;
        int segStart = 0;
        for (int iSeg = 0; iSeg < 10; iSeg++)
        {
            int segEnd = Math.Min(segStart + segLength, checkedItems.Count);

            VerifyPathSet(segStart, segEnd, pathSubst);
            segStart += segLength;

            if (segEnd == checkedItems.Count)
                break;
        }

        if (segStart < checkedItems.Count)
            VerifyPathSet(segStart, checkedItems.Count, pathSubst);
    }

    private void HandleDoubleClick(object sender, MouseButtonEventArgs e)
    {
        PseMediaItem? selected = mediaItemsListView.SelectedItem as PseMediaItem;

        if (selected != null)
        {
            MediaItemDetails details = new MediaItemDetails(selected);

            details.ShowDialog();
        }
    }
}
