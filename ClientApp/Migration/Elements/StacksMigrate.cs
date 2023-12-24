using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Thetacat.Logging;
using Thetacat.Migration.Elements.Media;
using Thetacat.Migration.Elements.Versions;
using Thetacat.Model;

namespace Thetacat.Migration.Elements;

public class StacksMigrate
{
    readonly ObservableCollection<PseStackItem> m_versionStacks = new ObservableCollection<PseStackItem>();
    readonly ObservableCollection<PseStackItem> m_mediaStacks = new ObservableCollection<PseStackItem>();

    public ObservableCollection<PseStackItem> VersionStacks => m_versionStacks;
    public ObservableCollection<PseStackItem> MediaStacks => m_mediaStacks;

    public StacksMigrate()
    {
    }

    public void SetVersionStacks(List<PseStackItem> stacks)
    {
        foreach (PseStackItem stack in stacks)
        {
            m_versionStacks.Add(stack);
        }
    }

    public void SetMediaStacks(List<PseStackItem> stacks)
    {
        foreach (PseStackItem stack in stacks)
        {
            m_mediaStacks.Add(stack);
        }
    }

    public void CreateCatStacks(MediaMigrate mediaMigrate)
    {
        AssociateMediaWithStacks(mediaMigrate, m_mediaStacks);
        AssociateMediaWithStacks(mediaMigrate, m_versionStacks);
    }

    private void AssociateMediaWithStacks(MediaMigrate mediaMigrate, IEnumerable<PseStackItem> stacks)
    {
        Dictionary<int, Guid> mapPseStackIdToCatStackId = new();

        foreach (PseStackItem item in stacks)
        {
            IPseMediaItem? pseItem = mediaMigrate.GetMediaFromPseId(item.MediaID);

            if (!mapPseStackIdToCatStackId.TryGetValue(item.StackID, out Guid catStackID))
            {
                catStackID = Guid.NewGuid();
                mapPseStackIdToCatStackId.Add(item.StackID, catStackID);
            }

            if (pseItem == null)
            {
                MainWindow.LogForApp(EventType.Error, $"pseItem {item.MediaID} not found for stack");
                continue;
            }

            if (!MainWindow._AppState.Catalog.Items.TryGetValue(pseItem.CatID, out MediaItem mediaItem))
            {
                MainWindow.LogForApp(EventType.Error, $"pseItem {pseItem.ID} ({pseItem.FullPath}) has not catalog item. migrate not done?");
                continue;
            }

            item.CatMediaId = mediaItem.ID;
            item.CatStackId = catStackID;
        }
    }
}
