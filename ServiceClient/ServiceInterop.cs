using Thetacat.ServiceClient.LocalService;

namespace Thetacat.ServiceClient;

public class ServiceInterop
{
    public ServiceMetatagSchema GetMetatagSchema()
    {
        return LocalService.Metatags.GetMetatagSchema();
    }
}
