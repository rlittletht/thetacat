using System;

namespace Thetacat.Model.Client;

public class OnDerivativeWorkCompleteArgs
{
    public Guid MediaId { get; set; }
    public string Md5 { get; set; }

    public OnDerivativeWorkCompleteArgs(Guid mediaId, string md5)
    {
        MediaId = mediaId;
        Md5 = md5;
    }
}
