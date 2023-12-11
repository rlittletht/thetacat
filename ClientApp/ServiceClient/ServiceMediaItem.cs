using System;

namespace Thetacat.ServiceClient;

public class ServiceMediaItem
{
    public Guid? Id { get; set; }
    public string? VirtualPath { get; set; }
    public string? MimeType { get; set; }
    public string? State { get; set; }
    public string? Sha5 { get; set; }
}
