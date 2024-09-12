using System.Collections.Generic;
using NUnit.Framework;

namespace Thetacat.ServiceClient;

public class ServiceDeletedItemsClock
{
    public List<ServiceDeletedItem> DeletedItems { get; set; } = new();
    public int? VectorClock { get; set; }
}
