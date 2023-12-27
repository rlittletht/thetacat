using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using NUnit.Framework;
using Thetacat.Logging;
using Thetacat.Migration.Elements.Metadata.UI;
using Thetacat.Model;
using Thetacat.Model.Metatags;
using Thetacat.Types;
using Thetacat.Util;

/*
MediaTags in the catalog are both the painted metatags as well as metadata that have
values. A lot of these tags come from the file itself when it is imported (its read
from the metadata directories). There's also a lot of metadata stored in the PSE database
(including the painted metatags).

This panel will show all of the data that needs to be migrated from PSE to the catalog, based
on the metatag mappins (on the standard and user tabs). Data in PSE that isn't selected for migrate
will just be dropped. Data that has a map to the catalog will be summarized here.

We have already read in all of the metadata and metatags from PSE and they are stored on each 
of the PseMediaItems. We will build a list of MediaTagMigrateItems from those items.
 */
namespace Thetacat.Migration.Elements.Media.UI;

/// <summary>
/// Interaction logic for MediaTagMigrateSummary.xaml
/// </summary>
public partial class MediaTagMigrateSummary : UserControl
{
    private readonly SortableListViewSupport m_sortableListViewSupport;
    private void SortType(object sender, RoutedEventArgs e) => m_sortableListViewSupport.Sort(sender as GridViewColumnHeader);

    private ElementsMigrate? m_migrate;

    private readonly ObservableCollection<MediaTagMigrateItem> m_mediatagMigrationItems = new();

    private ElementsMigrate _Migrate
    {
        get
        {
            if (m_migrate == null)
                throw new Exception($"initialize never called on {this.GetType().Name}");
            return m_migrate;
        }
    }

    public void Initialize(ElementsMigrate migrate)
    {
        m_migrate = migrate;
    }

    public MediaTagMigrateSummary()
    {
        InitializeComponent();
        m_sortableListViewSupport = new SortableListViewSupport(diffOpListView);
        diffOpListView.ItemsSource = m_mediatagMigrationItems;
    }

    // we have several properties are set as BUILTIN -- where do those get migrated? if we want those to be
    // columns in the media table, then they should get populated when we migrate the media (make sure we 
    // actually have applied the properties to the PseMediaItem...  but that sort of breaks the "migrate media
    // is just an import".  where does the import get this data from?
    // for things like width/height we could get it from the jpeg or other media directory, but can we get it
    // from the jp2? what about file data? get that from the file info?

    public void BuildSummary()
    {
        m_mediatagMigrationItems.Clear();

        foreach (PseMediaItem item in _Migrate.MediaMigrate.MediaItems)
        {
            if (!item.InCatalog)
            {
//                MainWindow.LogForApp(EventType.Error, $"can't build mediatags if item not in catalog. Media not migrated? {item.FullPath}");
                continue;
            }

            MediaItem catItem = MainWindow._AppState.Catalog.Items[item.CatID];

            // build the PSE metadata values to migrate
            foreach (PseMediaTagValue mediaTagValue in item.Metadata)
            {
                PseMetadata metadataItem = _Migrate.MetatagMigrate.MetadataSchema.LookupPseIdentifier(mediaTagValue.PseIdentifier);

                if (metadataItem.CatID == null)
                    continue;

                Metatag? metatag = MainWindow._AppState.MetatagSchema.FindFirstMatchingItem(
                    MetatagMatcher.CreateIdMatch(metadataItem.CatID.Value));

                if (metatag == null)
                    throw new CatExceptionInternalFailure($"can't find metatag {metadataItem.CatID.Value}");

                // see if this tag is already set on the media item
                if (catItem.Tags.TryGetValue(metadataItem.CatID.Value, out MediaTag? existing))
                {
                    // check to see if the values are the same (we won't change them, we will just log it
                    // and move on
                    if (existing.Value != null && existing.Value != mediaTagValue.Value)
                    {
                        MainWindow.LogForApp(
                            EventType.Warning,
                            $"metadata for {item.FullPath}:{metatag.Description} {existing.Value} != {mediaTagValue.Value}");
                    }
                    // since we already have the tag, just skip
                    continue;
                }

                m_mediatagMigrationItems.Add(new MediaTagMigrateItem(catItem, metatag, mediaTagValue.Value));
            }

            // and now do the metatags painted on the item
            foreach (PseMetatag mediaTagValue in item.Tags)
            {
                if (mediaTagValue.CatID == null)
                    continue;

                Metatag? metatag = MainWindow._AppState.MetatagSchema.FindFirstMatchingItem(
                    MetatagMatcher.CreateIdMatch(mediaTagValue.CatID.Value));

                if (metatag == null)
                    throw new CatExceptionInternalFailure($"can't find metatag {mediaTagValue.CatID.Value}");

                // see if this tag is already set on the media item. if it is, just skip (there are no values for these tags)
                if (catItem.Tags.TryGetValue(metatag.ID, out MediaTag? existing))
                    continue;

                m_mediatagMigrationItems.Add(new MediaTagMigrateItem(catItem, metatag, null));
            }
        }
    }

    /*----------------------------------------------------------------------------
        %%Function: DoMigrate
        %%Qualified: Thetacat.Migration.Elements.Media.UI.MediaTagMigrateSummary.DoMigrate

        Only migrate the checked items
    ----------------------------------------------------------------------------*/
    private void DoMigrate(object sender, RoutedEventArgs e)
    {
        MetatagSchemaDiff diff = MainWindow._AppState.MetatagSchema.BuildDiffForSchemas();

        if (!diff.IsEmpty)
        {
            MessageBox.Show("Can't migrate mediatags when there are schema changes that need to be committed");
            _Migrate.SwitchToSchemaSummariesTab();
            return;
        }
        
        List<MediaTagMigrateItem> checkedItems = CheckableListViewSupport<MediaTagMigrateItem>.GetCheckedItems(diffOpListView);

        foreach (MediaTagMigrateItem item in checkedItems)
        {
            if (!MainWindow._AppState.Catalog.Items.TryGetValue(item.MediaID, out MediaItem catItem))
            {
                MainWindow.LogForApp(EventType.Warning, $"can't find media item {item}");
            }

            MediaTag tag = new MediaTag(item.MetatagSetting, item.Value);

            catItem.FAddOrUpdateTag(tag);
        }

        BuildSummary();
    }

    private void DoKeyDown(object sender, KeyEventArgs e) => CheckableListViewSupport<MediaTagMigrateItem>.DoKeyDown(diffOpListView, sender, e);
}