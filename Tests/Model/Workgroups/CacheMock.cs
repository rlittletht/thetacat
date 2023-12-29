using System.Collections.Concurrent;
using Thetacat.Model;
using Thetacat.Model.Workgroups;
using Thetacat.TcSettings;
using Thetacat.Types;
using Thetacat.Util;

namespace Tests.Model.Workgroups;

public class CacheMock : Cache, ICache
{
    private IWorkgroup? m_workgroupOverride;

    public override IWorkgroup _Workgroup => m_workgroupOverride ?? throw new CatExceptionInitializationFailure();

    public override CacheType Type => CacheType.Workgroup;

    public void SetWorkgroup(IWorkgroup workgroup)
    {
        m_workgroupOverride = workgroup;
    }

    public ConcurrentQueue<MediaItem> Queue => m_cacheQueue;

    // we can't guarantee what order they will be in, but we can verify
    // that they are there

    // THIS IS A DESTRUCTIVE operation -- the queue will be empty when we are done
    public void VerifyQueueContains(IEnumerable<MediaItem> items)
    {
        HashSet<Guid> queued = new HashSet<Guid>();

        while (m_cacheQueue.TryDequeue(out MediaItem? item))
        {
            queued.Add(item.ID);
        }

        int count = 0;

        foreach (MediaItem item in items)
        {
            Assert.IsTrue(queued.Contains(item.ID));
            count++;
        }

        Assert.AreEqual(count, queued.Count);
    }
}
