using System.IO;
using NUnit.Framework.Constraints;

namespace Thetacat.Util;

// this could also be a full path, but not necessarily
public class PathSegment
{
    private readonly string m_segment;
    private string? m_local;

    public PathSegment(string segment)
    {
        m_segment = segment.Replace("\\", "/");
    }

    public static PathSegment? CreateFromString(string? segment)
    {
        return segment == null ? null : new PathSegment(segment);
    }

    public override string ToString() => m_segment;
    public string Local => m_local ??= m_segment.Replace("/", "\\");

    public static PathSegment Combine(PathSegment a, PathSegment b)
    {
        return new PathSegment(Path.Combine(a.Local, b.Local));
    }

    public PathSegment? GetPathRoot()
    {
        return PathSegment.CreateFromString(Path.GetPathRoot(Local));
    }

    public PathSegment GetRelativePath(PathSegment root)
    {
        return new PathSegment(Path.GetRelativePath(root.Local, Local));
    }
}
