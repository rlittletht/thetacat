using System;

namespace Thetacat.Types;

public interface IMetatag
{
    public Guid ID { get; }
    public string Name { get;}
    public string Description { get; }
    public string Standard { get; }
    public bool LocalOnly { get; }

    public Guid? Parent { get; }
}
