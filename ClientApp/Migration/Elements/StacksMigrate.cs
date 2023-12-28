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

    public List<StackMigrateSummaryItem> CreateCatStacks(MediaMigrate mediaMigrate)
    {
        List<StackMigrateSummaryItem> summary = new();

        CreateCatStacksForMissingStacks(mediaMigrate, m_mediaStacks, summary, false);
        CreateCatStacksForMissingStacks(mediaMigrate, m_versionStacks, summary, true);

        return summary;
    }

    public void UpdateStackWithCatStacks(MediaMigrate mediaMigrate, IEnumerable<PseStackItem> stack, bool versionStack)
    {
        foreach (PseStackItem stackItem in stack)
        {
            IPseMediaItem? pseItem = mediaMigrate.GetMediaFromPseId(stackItem.MediaID);
            if (pseItem == null)
                continue;

            if (!MainWindow._AppState.Catalog.Media.Items.TryGetValue(pseItem.CatID, out MediaItem? catItem))
                continue;

            stackItem.CatMediaId = catItem.ID;
            if (versionStack)
                stackItem.CatStackId = catItem.VersionStack;
            else
                stackItem.CatStackId = catItem.MediaStack;
        }
    }

    public void UpdateStacksWithCatStacks(MediaMigrate mediaMigrate)
    {
        UpdateStackWithCatStacks(mediaMigrate, m_versionStacks, true);
        UpdateStackWithCatStacks(mediaMigrate, m_mediaStacks, false);
    }

    private void CreateCatStacksForMissingStacks(MediaMigrate mediaMigrate, IEnumerable<PseStackItem> stacks, List<StackMigrateSummaryItem> summary, bool versionStack)
    {
        Dictionary<int, Guid> mapPseStackIdToCatStackId = new();

        foreach (PseStackItem item in stacks)
        {
            if (item.CatStackId != null)
                continue;

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

            if (!MainWindow._AppState.Catalog.Media.Items.TryGetValue(pseItem.CatID, out MediaItem mediaItem))
            {
                MainWindow.LogForApp(EventType.Error, $"pseItem {pseItem.ID} ({pseItem.FullPath}) has not catalog item. migrate not done?");
                continue;
            }


            item.CatMediaId = mediaItem.ID;
            item.CatStackId = catStackID;

            summary.Add(new StackMigrateSummaryItem(mediaItem.ID, catStackID, item.MediaIndex, versionStack ? "version" : "media", mediaItem.VirtualPath));
        }
    }
}
