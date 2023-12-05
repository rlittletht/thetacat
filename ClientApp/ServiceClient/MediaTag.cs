using System;

namespace Thetacat.ServiceClient;

/*----------------------------------------------------------------------------
    %%Class: MediaTag
    %%Qualified: Thetacat.ServiceClient.MediaTag

    This represents the data that comes back from the service for a mediatag
    (a metatag + its value).

    this will be stored with the MediaItem in our model, so when we create
    the model MediaTag, it won't have its MediaId set (since it will be
    related to its MediaItem by its parent/child relationship
----------------------------------------------------------------------------*/
public class MediaTag
{
    public Guid MediaId { get; set; } = Guid.Empty;
    public Guid Id { get; set; } = Guid.Empty;
    public string? Value { get; init; }
}
