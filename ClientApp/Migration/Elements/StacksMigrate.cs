using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Thetacat.Logging;
using Thetacat.Migration.Elements.Media;
using Thetacat.Migration.Elements.Versions;
using Thetacat.Model;
using Thetacat.Types;

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
        m_versionStacks.Clear();
        foreach (PseStackItem stack in stacks)
        {
            m_versionStacks.Add(stack);
        }
    }

    public void SetMediaStacks(List<PseStackItem> stacks)
    {
        m_mediaStacks.Clear();
        foreach (PseStackItem stack in stacks)
        {
            m_mediaStacks.Add(stack);
        }
    }

    public List<StackMigrateSummaryItem> CreateCatStacks(MediaMigrate mediaMigrate)
    {
        List<StackMigrateSummaryItem> summary = new();

        CreateCatStacksForMissingStacks(mediaMigrate, m_mediaStacks, summary, MediaStackType.Media);
        CreateCatStacksForMissingStacks(mediaMigrate, m_versionStacks, summary, MediaStackType.Version);

        return summary;
    }

    public void UpdateStackWithCatStacks(MediaMigrate mediaMigrate, IEnumerable<PseStackItem> stack, MediaStackType stackType)
    {
        foreach (PseStackItem stackItem in stack)
        {
            stackItem.CatMediaId = null;
            stackItem.CatStackId = null;

            IPseMediaItem? pseItem = mediaMigrate.GetMediaFromPseId(stackItem.MediaID);
            if (pseItem == null)
                continue;

            if (!App.State.Catalog.TryGetMedia(pseItem.CatID, out MediaItem? catItem))
                continue;

            stackItem.CatMediaId = catItem.ID;
            if (stackType.Equals(MediaStackType.Version))
                stackItem.CatStackId = catItem.VersionStack;
            else if (stackType.Equals(MediaStackType.Media))
                stackItem.CatStackId = catItem.MediaStack;
            else 
                throw new CatExceptionInternalFailure("unknown stack type");

            if (stackItem.CatStackId != null)
                mapPseStackIdToCatStackId.TryAdd(stackItem.StackID, stackItem.CatStackId.Value);
        }
    }

    public void UpdateStacksWithCatStacks(MediaMigrate mediaMigrate)
    {
        mapPseStackIdToCatStackId.Clear();
        UpdateStackWithCatStacks(mediaMigrate, m_versionStacks, MediaStackType.Version);
        UpdateStackWithCatStacks(mediaMigrate, m_mediaStacks, MediaStackType.Media);
    }

    readonly Dictionary<int, Guid> mapPseStackIdToCatStackId = new();

    private void CreateCatStacksForMissingStacks(MediaMigrate mediaMigrate, IEnumerable<PseStackItem> stacks, List<StackMigrateSummaryItem> summary, MediaStackType stackType)
    {
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
                App.LogForApp(EventType.Error, $"pseItem {item.MediaID} not found for stack");
                continue;
            }

            if (!App.State.Catalog.TryGetMedia(pseItem.CatID, out MediaItem? mediaItem))
            {
                App.LogForApp(EventType.Error, $"pseItem {pseItem.ID} ({pseItem.FullPath}) has not catalog item. migrate not done?");
                continue;
            }

            item.CatMediaId = mediaItem.ID;
            item.CatStackId = catStackID;

            summary.Add(new StackMigrateSummaryItem(mediaItem.ID, catStackID, item.MediaIndex, stackType, mediaItem.VirtualPath));
        }
    }
}
