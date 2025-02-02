using System;

namespace Thetacat.ServiceClient;

/*----------------------------------------------------------------------------
    %%Class: ServiceMediaTag
    %%Qualified: Thetacat.ServiceClient.ServiceMediaTag

    This represents the data that comes back from the service for a mediatag
    (a metatag + its value).

    this will be stored with the MediaItem in our model, so when we create
    the model ServiceMediaTag, it won't have its MediaId set (since it will be
    related to its MediaItem by its parent/child relationship
----------------------------------------------------------------------------*/
public class ServiceMediaTag
{
    public Guid MediaId { get; set; } = Guid.Empty;
    public Guid Id { get; set; } = Guid.Empty;
    public string? Value { get; set; }
    public int Clock { get; set; } = 0;
    public bool Deleted { get; set; } = false;
}
