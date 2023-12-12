using System.Collections.Generic;

namespace Thetacat.ServiceClient;

public class ServiceCatalog
{
    public List<ServiceMediaItem>? MediaItems { get; set; }
    public List<ServiceMediaTag>? MediaTags { get; set; }
}
