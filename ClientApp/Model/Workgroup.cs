using System;
using Thetacat.ServiceClient;
using Thetacat.Util;

namespace Thetacat.Model;

public class Workgroup
{
    private Guid m_id;

    public string Name { get; }
    public PathSegment Server { get; }
    public PathSegment CacheRoot { get; }

    public string FullyQualifiedPath => PathSegment.Combine(Server, CacheRoot).Local;

    public Workgroup(Guid id)
    {
        m_id = id;

        // FUTURE: consider using cached workgroup details from settings if the
        // connection fails to the server?
        ServiceWorkgroup serviceWorkgroup = ServiceInterop.GetWorkgroupDetails(id);
        Name = serviceWorkgroup.Name ?? throw new InvalidOperationException("no name from server");
        Server = PathSegment.CreateFromString(serviceWorkgroup.ServerPath) ?? throw new InvalidOperationException("no servername from server");
        CacheRoot = PathSegment.CreateFromString(serviceWorkgroup.CacheRoot) ?? throw new InvalidOperationException("no name from server");
    }
}
