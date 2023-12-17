using System.Collections.Concurrent;
using Thetacat.Model;
using Thetacat.Model.Workgroups;
using Thetacat.TcSettings;
using Thetacat.Types;
using Thetacat.Util;

namespace Tests.Model.Workgroups;

public class CacheMock: Cache, ICache
{
    private IWorkgroup? m_workgroupOverride;

    public new IWorkgroup _Workgroup => m_workgroupOverride ?? throw new CatExceptionInitializationFailure();

    public new CacheType Type => CacheType.Workgroup;

    public void SetWorkgroup(IWorkgroup workgroup)
    {
        m_workgroupOverride = workgroup;
    }

    public ConcurrentQueue<MediaItem> Queue => base.m_cacheQueue;

}
