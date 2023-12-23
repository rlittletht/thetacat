using System.Collections.Generic;
using System.Collections.ObjectModel;
using Thetacat.Logging;
using Thetacat.Migration.Elements.Media;
using Thetacat.Migration.Elements.Versions;

namespace Thetacat.Migration.Elements;

public class StacksMigrate
{
    readonly ObservableCollection<PseVersionStackItem> m_stacks = new ObservableCollection<PseVersionStackItem>();

    public ObservableCollection<PseVersionStackItem> Stacks => m_stacks;
    public StacksMigrate()
    {
    }

    public void UpdateStackItemsFromMediaSet(MediaMigrate media)
    {
        foreach (PseVersionStackItem item in m_stacks)
        {
            IPseMediaItem? mediaItem = media.GetMediaFromPseId(item.MediaID);

            if (mediaItem == null)
            {
                MainWindow.LogForApp(EventType.Error, $"version stack {item.StackID} references unknown media id {item.MediaID}");
                continue;
            }

            item.CreateDate = mediaItem.FileDateOriginal;
        }
    }

    public void SetVersionStacks(List<PseVersionStackItem> stacks)
    {
        foreach (PseVersionStackItem stack in stacks)
        {
            m_stacks.Add(stack);
        }
    }
}
