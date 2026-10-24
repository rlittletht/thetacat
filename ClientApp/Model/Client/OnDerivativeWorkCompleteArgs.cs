using System;
using Thetacat.Util;

namespace Thetacat.Model.Client;

public class OnDerivativeWorkCompleteArgs
{
    public Guid MediaId { get; set; }
    public string Md5 { get; set; }
    public PathSegment DerivativePath { get; set; }
    
    public OnDerivativeWorkCompleteArgs(Guid mediaId, string md5, PathSegment derivativePath)
    {
        MediaId = mediaId;
        Md5 = md5;
        DerivativePath = derivativePath;
    }
}
