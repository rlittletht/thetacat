using System;

namespace Thetacat.ServiceClient.LocalDatabase;

public class DerivativeDbItem
{
    public Guid MediaId { get; set; }
    public string MimeType { get; set; }
    public double ScaleFactor { get; set; }
    public string Path { get; set; }
    public string TransformationsKey { get; set; }

    public DerivativeDbItem(Guid mediaId, string mimeType, double scaleFactor, string transformationsKey, string path)
    {
        MediaId = mediaId;
        MimeType = mimeType;
        ScaleFactor = scaleFactor;
        Path = path;
        TransformationsKey = transformationsKey;
    }
}
