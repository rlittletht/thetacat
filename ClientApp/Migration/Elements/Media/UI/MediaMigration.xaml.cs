﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Thetacat.Import;
using Thetacat.Logging;
using Thetacat.Migration.Elements.Media;
using Thetacat.Migration.Elements.Media.UI;
using Thetacat.Migration.Elements.Metadata.UI.Media;
using Thetacat.Model;
using Thetacat.ServiceClient.LocalDatabase;
using Thetacat.Types;
using Thetacat.UI;
using Thetacat.Util;

namespace Thetacat.Migration.Elements.Metadata.UI;

/// <summary>
/// Interaction logic for MediaMigration.xaml
/// </summary>
public partial class MediaMigration : UserControl
{
    private ElementsMigrate? m_migrate;

    private readonly MediaMigrationModel m_model = new();

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
        m_model.VerifyMD5 = true;
        DataContext = m_model;
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
        _Migrate.MediaMigrate.PropagateMetadataToBuiltins(_Migrate.MetatagMigrate);
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
        foreach(TcSettings.TcSettings.MapPair subst in App.State.ActiveProfile.ElementsSubstitutions)
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
            verifyTimer?.Stop();
            App.LogForApp(EventType.Information, $"VerifyPaths: {verifyTimer?.Elapsed()}");
            App.State.Md5Cache.CommitCacheItems();
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

        if (!m_model.VerifyMD5)
        {
            MessageBox.Show(
                "You are verifying paths WITHOUT checking MD5 hashes. This will only check against the virtual path in the catalog which is inherently imprecise. You make miss some media imports because of false path matches.");
        }
        Task.Run(
                () =>
                {
                    for (int i = start; i < end; i++)
                    {
                        items[i].CheckPath(subs, m_model.VerifyMD5);
                    }
                })
           .ContinueWith(delegate { CompleteVerifyTask(); }, uiScheduler);
    }

    /*----------------------------------------------------------------------------
        %%Function: BuildCheckedVerifiedItems
        %%Qualified: Thetacat.Migration.Elements.Metadata.UI.MediaMigration.BuildCheckedVerifiedItems
    ----------------------------------------------------------------------------*/
    private List<PseMediaItem> BuildCheckedVerifiedItems()
    {
        return CheckableListViewSupport<PseMediaItem>.GetCheckedItems(
            mediaItemsListView,
            (PseMediaItem item) => item.PathVerified == TriState.Yes && item.InCatalog == false);
    }

    private MicroTimer? verifyTimer;
    /*----------------------------------------------------------------------------
        %%Function: VerifyPaths
        %%Qualified: Thetacat.Migration.Elements.Metadata.UI.MediaMigration.VerifyPaths
    ----------------------------------------------------------------------------*/
    public void VerifyPaths()
    {
        Dictionary<string, string> pathSubst = new();

        List<string> regValues = new();

        App.State.ActiveProfile.ElementsSubstitutions.Clear();
        foreach (PathSubstitution sub in _Migrate.MediaMigrate.PathSubstitutions)
        {
            App.State.ActiveProfile.ElementsSubstitutions.Add(
                new TcSettings.TcSettings.MapPair()
                {
                    From = sub.From,
                    To = sub.To,
                });
            pathSubst.Add(sub.From, sub.To);
        }

        App.State.Settings.WriteSettings();

        ((Storyboard?)VerifyStatus.Resources.FindName("spinner"))?.Begin();

        VerifyResult.Visibility = Visibility.Hidden;
        VerifyStatus.Visibility = Visibility.Visible;

        // build the list to check (only the marked items)
        List<PseMediaItem> checkedItems = 
            CheckableListViewSupport<PseMediaItem>.GetCheckedItems(mediaItemsListView);

        verifyTimer = new MicroTimer();
        verifyTimer.Start();

        // split the list into 4 parts and do them in parallel
        int segCount = 4;
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
            PseMediaItemDetails details = new PseMediaItemDetails(selected);

            details.ShowDialog();
        }
    }

    private void DoRemainingPrePopulateWork(IProgressReport report, List<PseMediaItem> checkedItems)
    {
        int i = 0, iMax = checkedItems.Count;

        foreach (PseMediaItem item in checkedItems)
        {
            report.UpdateProgress((i++ * 100.0) / iMax);
            item.UpdateCatalogStatus(false /*verifyMd5*/);
//
//            // here we can pre-populate our cache.
//            MediaItem mediaItem = App.State.Catalog.GetMediaFromId(item.CatID);
//            App.State.Cache.PrimeCacheFromImport(mediaItem, item.VerifiedPath ?? throw new CatExceptionInternalFailure());
//            mediaItem.NotifyCacheStatusChanged();
        }
        report.WorkCompleted();
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
        MediaImporter importer = new MediaImporter(
            checkedItems, 
            MainApp.MainWindow.ClientName,
            (itemFile, catalogItem) =>
            {
                PseMediaItem pseItem = itemFile as PseMediaItem ?? throw new CatExceptionInternalFailure("file item isn't a PseMediaItem");
                pseItem.CatID = catalogItem.ID;
            });

        importer.CreateCatalogItemsAndUpdateImportTable(App.State.ActiveProfile.CatalogID, App.State.Catalog, App.State.MetatagSchema, App.State.Cache);
        ProgressDialog.DoWorkWithProgress(report => DoRemainingPrePopulateWork(report, checkedItems), Window.GetWindow(this));

        // and lastly we have to add the items we just manually added to our cache
        // (we don't have any items we are tracking. these should all be adds)
        App.State.Cache.PushChangesToDatabase(null);
        _Migrate.ReloadSchemas();
    }

    private void MigrateMetadata(object sender, RoutedEventArgs e)
    {

    }

    private void DoKeyDown(object sender, KeyEventArgs e) => CheckableListViewSupport<PseMediaItem>.DoKeyDown(mediaItemsListView, sender, e);
}
