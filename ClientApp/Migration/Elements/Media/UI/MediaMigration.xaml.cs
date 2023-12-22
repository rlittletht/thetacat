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
using Thetacat.Import;
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
    private ElementsMigrate? m_migrate;

    private ElementsMigrate _Migrate
    {
        get
        {
            if (m_migrate == null)
                throw new Exception($"initialize never called on {this.GetType().Name}");
            return m_migrate;
        }
    }

    /*----------------------------------------------------------------------------
        %%Function: MediaMigration
        %%Qualified: Thetacat.Migration.Elements.Metadata.UI.MediaMigration.MediaMigration
    ----------------------------------------------------------------------------*/
    public MediaMigration()
    {
        InitializeComponent();
    }

    /*----------------------------------------------------------------------------
        %%Function: VerifyPaths
        %%Qualified: Thetacat.Migration.Elements.Metadata.UI.MediaMigration.VerifyPaths
    ----------------------------------------------------------------------------*/
    private void VerifyPaths(object sender, RoutedEventArgs e)
    {
        VerifyPaths();
    }

    private void ShowCount(object sender, RoutedEventArgs e)
    {
        MessageBox.Show($"Total count: {_Migrate.MediaMigrate.MediaItems.Count}");
    }
    /*----------------------------------------------------------------------------
        %%Function: DoFilterItemChanged
        %%Qualified: Thetacat.Migration.Elements.Metadata.UI.MediaMigration.DoFilterItemChanged
    ----------------------------------------------------------------------------*/
    private void DoFilterItemChanged(object sender, SelectionChangedEventArgs e)
    {
        CollectionViewSource.GetDefaultView(mediaItemsListView.ItemsSource).Refresh();
    }

    /*----------------------------------------------------------------------------
        %%Function: Initialize
        %%Qualified: Thetacat.Migration.Elements.Metadata.UI.MediaMigration.Initialize
    ----------------------------------------------------------------------------*/
    public void Initialize(ElementsDb db, ElementsMigrate migrate)
    {
        m_migrate = migrate;

        _Migrate.MediaMigrate.SetMediaItems(new List<PseMediaItem>(db.ReadMediaItems(_Migrate.MetatagMigrate)));

        mediaItemsListView.ItemsSource = _Migrate.MediaMigrate.MediaItems;

        CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(mediaItemsListView.ItemsSource);
        view.Filter = FilterMediaItem;

        SetSubstitutionsFromSettings();
    }

    /*----------------------------------------------------------------------------
        %%Function: FilterMediaItem
        %%Qualified: Thetacat.Migration.Elements.Metadata.UI.MediaMigration.FilterMediaItem
    ----------------------------------------------------------------------------*/
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

    /*----------------------------------------------------------------------------
        %%Function: SetSubstitutionsFromSettings
        %%Qualified: Thetacat.Migration.Elements.Metadata.UI.MediaMigration.SetSubstitutionsFromSettings
    ----------------------------------------------------------------------------*/
    public void SetSubstitutionsFromSettings()
    {
        foreach(TcSettings.TcSettings.MapPair subst in MainWindow._AppState.Settings.ElementsSubstitutions)
//        foreach (string s in _AppState.Settings.Settings.RgsValue("LastElementsSubstitutions"))
        {
//            string[] pair = s.Split(",");
//            if (pair.Length != 2)
//            {
//                MessageBox.Show($"bad subst setting in registry: {s}");
//                continue;
//            }
//
            _Migrate.MediaMigrate.PathSubstitutions.Add(new PathSubstitution { From = subst.From, To = subst.To });
        }

        substDatagrid.ItemsSource = _Migrate.MediaMigrate.PathSubstitutions;
    }

    private int m_countRunningVerifyTasks = 0;

    /*----------------------------------------------------------------------------
        %%Function: SetVerifyResult
        %%Qualified: Thetacat.Migration.Elements.Metadata.UI.MediaMigration.SetVerifyResult
    ----------------------------------------------------------------------------*/
    void SetVerifyResult()
    {
        TriState tri = TriState.Maybe;

        foreach (PseMediaItem item in _Migrate.MediaMigrate.MediaItems)
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

    /*----------------------------------------------------------------------------
        %%Function: CompleteVerifyTask
        %%Qualified: Thetacat.Migration.Elements.Metadata.UI.MediaMigration.CompleteVerifyTask
    ----------------------------------------------------------------------------*/
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

    /*----------------------------------------------------------------------------
        %%Function: VerifyPathSet
        %%Qualified: Thetacat.Migration.Elements.Metadata.UI.MediaMigration.VerifyPathSet
    ----------------------------------------------------------------------------*/
    void VerifyPathSet(List<PseMediaItem> items, int start, int end, Dictionary<string, string> subs)
    {
        Interlocked.Increment(ref m_countRunningVerifyTasks);
        TaskScheduler uiScheduler = TaskScheduler.FromCurrentSynchronizationContext();

        Task.Run(
                () =>
                {
                    for (int i = start; i < end; i++)
                    {
                        items[i].CheckPath(subs);
                    }
                })
           .ContinueWith(delegate { CompleteVerifyTask(); }, uiScheduler);
    }

    private List<PseMediaItem> BuildCheckedItems()
    {
        // build the list to check (only the marked items)
        List<PseMediaItem> checkedItems = new List<PseMediaItem>();
        foreach (PseMediaItem item in _Migrate.MediaMigrate.MediaItems)
        {
            if (item.Migrate)
                checkedItems.Add(item);
        }

        return checkedItems;
    }

    /*----------------------------------------------------------------------------
        %%Function: BuildCheckedVerifiedItems
        %%Qualified: Thetacat.Migration.Elements.Metadata.UI.MediaMigration.BuildCheckedVerifiedItems
    ----------------------------------------------------------------------------*/
    private List<PseMediaItem> BuildCheckedVerifiedItems()
    {
        // build the list to check (only the marked items)
        List<PseMediaItem> checkedItems = new List<PseMediaItem>();
        foreach (PseMediaItem item in _Migrate.MediaMigrate.MediaItems)
        {
            if (item.Migrate && item.PathVerified == TriState.Yes && item.InCatalog == false)
                checkedItems.Add(item);
        }

        return checkedItems;
    }

    /*----------------------------------------------------------------------------
        %%Function: VerifyPaths
        %%Qualified: Thetacat.Migration.Elements.Metadata.UI.MediaMigration.VerifyPaths
    ----------------------------------------------------------------------------*/
    public void VerifyPaths()
    {
        Dictionary<string, string> pathSubst = new();

        List<string> regValues = new();

        MainWindow._AppState.Settings.ElementsSubstitutions.Clear();
        foreach (PathSubstitution sub in _Migrate.MediaMigrate.PathSubstitutions)
        {
            MainWindow._AppState.Settings.ElementsSubstitutions.Add(
                new TcSettings.TcSettings.MapPair()
                {
                    From = sub.From,
                    To = sub.To,
                });
            pathSubst.Add(sub.From, sub.To);
        }

        MainWindow._AppState.Settings.WriteSettings();

        ((Storyboard?)VerifyStatus.Resources.FindName("spinner"))?.Begin();

        VerifyResult.Visibility = Visibility.Hidden;
        VerifyStatus.Visibility = Visibility.Visible;

        // build the list to check (only the marked items)
        List<PseMediaItem> checkedItems = BuildCheckedItems();

        // split the list into 4 parts and do them in parallel
        int segCount = 10;
        int segLength = checkedItems.Count / segCount;
        int segStart = 0;
        for (int iSeg = 0; iSeg < segCount; iSeg++)
        {
            int segEnd = Math.Min(segStart + segLength, checkedItems.Count);

            VerifyPathSet(checkedItems, segStart, segEnd, pathSubst);
            segStart += segLength;

            if (segEnd == checkedItems.Count)
                break;
        }

        if (segStart < checkedItems.Count)
            VerifyPathSet(checkedItems, segStart, checkedItems.Count, pathSubst);
    }

    /*----------------------------------------------------------------------------
        %%Function: HandleDoubleClick
        %%Qualified: Thetacat.Migration.Elements.Metadata.UI.MediaMigration.HandleDoubleClick
    ----------------------------------------------------------------------------*/
    private void HandleDoubleClick(object sender, MouseButtonEventArgs e)
    {
        PseMediaItem? selected = mediaItemsListView.SelectedItem as PseMediaItem;

        if (selected != null)
        {
            MediaItemDetails details = new MediaItemDetails(selected);

            details.ShowDialog();
        }
    }

    /*----------------------------------------------------------------------------
        %%Function: MigrateToCatalog
        %%Qualified: Thetacat.Migration.Elements.Metadata.UI.MediaMigration.MigrateToCatalog

        Take the checked items and add them to the catalog (and mark them pending
        for upload). This can only happen on items with verified paths
    ----------------------------------------------------------------------------*/
    private void MigrateToCatalog(object sender, RoutedEventArgs e)
    {
        List<PseMediaItem> checkedItems = BuildCheckedVerifiedItems();
        MediaImport import = new MediaImport(checkedItems, MainWindow.ClientName);

        import.CreateCatalogItemsAndUpdateImportTable(MainWindow._AppState.Catalog, MainWindow._AppState.MetatagSchema);
        foreach (PseMediaItem item in checkedItems)
        {
            item.UpdateCatalogStatus();
        }
    }

    private void MigrateMetadata(object sender, RoutedEventArgs e)
    {

    }

    /*----------------------------------------------------------------------------
        %%Function: DoKeyDown
        %%Qualified: Thetacat.Migration.Elements.Metadata.UI.MediaMigration.DoKeyDown

        we might have to do something special here to prevent it from deselecting
        our selection when space is pressed
    ----------------------------------------------------------------------------*/
    private void DoKeyDown(object sender, KeyEventArgs e)
    {
        if (!e.IsRepeat && e.Key == Key.Space)
        {
            bool notMixed = mediaItemsListView.SelectedItems.Cast<object>().Any(item => ((PseMediaItem)item).Migrate)
                ^ mediaItemsListView.SelectedItems.Cast<object>().Any(item => !((PseMediaItem)item).Migrate);

            foreach (object? item in mediaItemsListView.SelectedItems)
            {
                if (item is PseMediaItem pseItem)
                    pseItem.Migrate = !notMixed || !pseItem.Migrate;
            }
        }
    }
}
