using System;
using System.Collections.Generic;

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
public class ServiceMediaTagsWithClocks
{
    public List<ServiceMediaTag> Tags { get; set; } = new();
    public int TagClock { get; set; } = 0;
    public int ResetClock { get; set; } = 0;
}
