using System.Collections.Concurrent;
using Thetacat.Model;
using Thetacat.Model.Workgroups;
using Thetacat.Types;

namespace Tests.Model.Workgroups;

public class WorkgroupMock: Workgroup, IWorkgroup
{
    public delegate ServiceWorkgroupMediaClock GetWorkgroupMediaClockDelegate();
    public delegate Dictionary<Guid, MediaItem> GetNextItemsForQueueDelegate();

    private GetWorkgroupMediaClockDelegate? m_getMediaClockDelegate;
    private GetNextItemsForQueueDelegate? m_getNextItemsForQueueDelegate;

    public void SetMediaClockSource(GetWorkgroupMediaClockDelegate getDelegate)
    {
        m_getMediaClockDelegate = getDelegate;
    }

    public void SetItemsForQueueSource(GetNextItemsForQueueDelegate getDelegate)
    {
        m_getNextItemsForQueueDelegate = getDelegate;
    }

    public new void RefreshWorkgroupMedia(ConcurrentDictionary<Guid, ICacheEntry> entries)
    {
        if (m_getMediaClockDelegate == null)
            return;

        ServiceWorkgroupMediaClock mediaClock = m_getMediaClockDelegate();

        UpdateFromWorkgroupMediaClock(entries, mediaClock);
    }

    public new Dictionary<Guid, MediaItem> GetNextItemsForQueue(int count)
    {
        if (m_getNextItemsForQueueDelegate == null)
            return new Dictionary<Guid, MediaItem>();

        return m_getNextItemsForQueueDelegate();
    }
}
