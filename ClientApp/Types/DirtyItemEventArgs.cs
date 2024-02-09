using System;

namespace Thetacat.Types;

public class DirtyItemEventArgs<T> : EventArgs
{
    public T Item { get; set; }

    public DirtyItemEventArgs(T t)
    {
        Item = t;
    }
}
