using System.Collections;
using System.Collections.Generic;

namespace Thetacat.Types;

public class ReadOnlyCollectionWrapper<T>: IReadOnlyCollection<T>
{
    private ICollection<T> collection;

    public ReadOnlyCollectionWrapper(ICollection<T> collection)
    {
        this.collection = collection;
    }

    public int Count => collection.Count;

    public IEnumerator<T> GetEnumerator()
    {
        return collection.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return collection.GetEnumerator();
    }
}

public static class ReadOnlyCollectionWrapper
{
    public static IReadOnlyCollection<T> AsReadOnly<T>(this ICollection<T> collection)
    {
        return new ReadOnlyCollectionWrapper<T>(collection);
    }
}
