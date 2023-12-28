using System;
using Thetacat.Model;
using Thetacat.Types.Parallel;

namespace Thetacat.Types;

public interface IMedia
{
    public ObservableConcurrentDictionary<Guid, MediaItem> Items { get; }
}
