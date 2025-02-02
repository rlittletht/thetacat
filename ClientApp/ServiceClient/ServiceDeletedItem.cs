using System;

namespace Thetacat.ServiceClient;

public class ServiceDeletedItem
{
    public Guid? Id { get; set; }
    public int? MinVectorClock { get; set; }
}
