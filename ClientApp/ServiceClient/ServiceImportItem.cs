using System;

namespace Thetacat.ServiceClient;

public class ServiceImportItem
{
    public Guid ID { get; set; }
    public string? State { get; set; }
    public string? SourcePath { get; set; }
    public string? SourceServer { get; set; }
    public DateTime? UploadDate { get; set; }
    public string? Source { get; set; }
}
