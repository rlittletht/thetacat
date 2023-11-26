using Thetacat.Model;
using Thetacat.ServiceClient.LocalService;

namespace Thetacat.ServiceClient;

public class ServiceInterop
{
    public static MetatagSchema GetMetatagSchema()
    {
        return MetatagSchema.CreateFromService(LocalService.Metatags.GetMetatagSchema());
    }
}
