namespace Thetacat.Types;

public interface IMetatagMatcher<T>
{
    public bool IsMatch(T item);
}
