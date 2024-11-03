using System.Collections.Concurrent;
using TCore.SqlCore;
using Thetacat.Model;
using Thetacat.Model.Workgroups;
using Thetacat.Types;

namespace Tests.Model.Workgroups;

public class WorkgroupMock : Workgroup, IWorkgroup
{
    public delegate ServiceWorkgroupMediaClock GetWorkgroupMediaClockDelegate();
    public delegate Dictionary<Guid, MediaItem> GetNextItemsForQueueDelegate();

    private GetWorkgroupMediaClockDelegate? m_getMediaClockDelegate;
    private GetNextItemsForQueueDelegate? m_getNextItemsForQueueDelegate;

    public WorkgroupMock(ISql sqlSource, Guid clientId) : base(sqlSource, clientId)
    {
    }

    public void SetMediaClockSource(GetWorkgroupMediaClockDelegate getDelegate)
    {
        m_getMediaClockDelegate = getDelegate;
    }

    public void SetItemsForQueueSource(GetNextItemsForQueueDelegate getDelegate)
    {
        m_getNextItemsForQueueDelegate = getDelegate;
    }

    public override void RefreshWorkgroupMedia(ConcurrentDictionary<Guid, ICacheEntry> entries)
    {
        if (m_getMediaClockDelegate == null)
        {
            base.RefreshWorkgroupMedia(entries);
            return;
        }

        ServiceWorkgroupMediaClock mediaClock = m_getMediaClockDelegate();

        UpdateFromWorkgroupMediaClock(entries, mediaClock);
    }

    public new Dictionary<Guid, MediaItem> GetNextItemsForQueueFromMediaCollection(Guid catalogID, IEnumerable<MediaItem> mediaCollection, ICache cache, int count)
    {
        if (m_getNextItemsForQueueDelegate == null)
            return base.GetNextItemsForQueueFromMediaCollection(catalogID, mediaCollection, cache, count);

        throw new NotImplementedException();
        // return m_getNextItemsForQueueDelegate();
    }

    public new Dictionary<Guid, MediaItem> GetNextItemsForQueue(int count)
    {
        if (m_getNextItemsForQueueDelegate == null)
            return base.GetNextItemsForQueue(count);

        return m_getNextItemsForQueueDelegate();
    }
}
