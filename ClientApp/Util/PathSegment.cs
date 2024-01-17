using System;
using System.IO;
using NUnit.Framework.Constraints;
using Thetacat.Model;
using Thetacat.Types;

namespace Thetacat.Util;

// this could also be a full path, but not necessarily
public class PathSegment
{
    private readonly string m_segment;
    private string? m_local;

    public PathSegment()
    {
        m_segment = string.Empty;
    }

    public PathSegment(PathSegment segmentIn)
    {
        m_segment = segmentIn.m_segment;
        m_local = segmentIn.m_local;
    }

    public PathSegment(string segmentIn, string? local)
    {
        m_segment = segmentIn;
        m_local = local;
    }

    public PathSegment(string segment)
    {
        m_segment = segment.Replace("\\", "/");
    }

    public static PathSegment CreateFromString(string? segment)
    {
        return segment == null ? PathSegment.Empty : new PathSegment(segment);
    }

    public override string ToString() => m_segment;
    public string Local => m_local ??= m_segment.Replace("/", "\\");

    public static PathSegment Join(PathSegment a, params PathSegment[] paths)
    {
        foreach (PathSegment path in paths)
        {
            a = new PathSegment(Path.Join(a.Local, path.Local));
        }

        return a;
    }

    public static PathSegment Join(string a, params string[] paths)
    {
        PathSegment segment = new PathSegment(a);

        foreach (string path in paths)
        {
            segment = new PathSegment(Path.Join(segment.Local, path));
        }

        return segment;
    }

    public PathSegment Unroot()
    {
        PathSegment root = GetPathRoot(this);

        if (root != PathSegment.Empty)
            return GetRelativePath(root, this);

        return this;
    }

    public PathSegment? GetPathRoot()
    {
        return PathSegment.CreateFromString(Path.GetPathRoot(Local));
    }

    public PathSegment GetRelativePath(PathSegment root)
    {
        return new PathSegment(Path.GetRelativePath(root.Local, Local));
    }

    public static PathSegment GetRelativePath(PathSegment? pathRoot, PathSegment path)
    {
        return new PathSegment(Path.GetRelativePath(pathRoot?.Local ?? string.Empty, path.Local));
    }

    public static PathSegment GetRelativePath(PathSegment pathRoot, string path)
    {
        return new PathSegment(Path.GetRelativePath(pathRoot.Local, path));
    }

    public static PathSegment GetPathRoot(PathSegment path)
    {
        return CreateFromString(Path.GetPathRoot(path.Local));
    }

    public static PathSegment GetPathRoot(string path)
    {
        return CreateFromString(Path.GetPathRoot(path));
    }

    public static implicit operator string(PathSegment path) => path.ToString();

    public static PathSegment Empty => new PathSegment(string.Empty);

    public PathSegment AppendLeafSuffix(string suffix)
    {
        string local = this.Local;

        string ext = Path.GetExtension(local);

        // NOTE: we can't use ChangeExtension to put the extension back because then foo.txt.jpg -> foo.txt -> foo.jpg

        return new PathSegment($"{Path.ChangeExtension(local, null)}{suffix}{ext}");
    }

    public PathSegment Clone()
    {
        return new PathSegment(this);
    }

    public bool HasDirectory()
    {
        return m_segment.Contains("/");
    }

    public delegate bool TraverseDelegate(PathSegment segment);

    /*----------------------------------------------------------------------------
        %%Function: TraverseDirectories
        %%Qualified: Thetacat.Util.PathSegment.TraverseDirectories

        delegate should return false if we should not continue to traverse
    ----------------------------------------------------------------------------*/
    public void TraverseDirectories(TraverseDelegate traverseItem)
    {
        string? root = Path.GetPathRoot(Local);
        string rootedDirectory = string.IsNullOrEmpty(root) ? Local : Path.GetRelativePath(root, Local);

        PathSegment current = new(rootedDirectory);
        while (current.HasDirectory())
        {
            if (!traverseItem(current))
                return;

            int ich = current.m_segment.IndexOf("/") + 1;

            if (ich == -1)
                throw new CatExceptionInternalFailure("no directory separator with a directory present?");
            current = new(current.m_segment[ich..]);
        }
    }

    public static bool operator ==(PathSegment? left, PathSegment? right)
    {
        if (ReferenceEquals(left, null) && ReferenceEquals(right, null)) return true;
        if (ReferenceEquals(left, null) || ReferenceEquals(right, null)) return false;

        if (string.Compare(left.m_segment, right.m_segment, StringComparison.CurrentCultureIgnoreCase) != 0) return false;
        if (left.m_local != null && right.m_local != null && string.Compare(left.m_local, right.m_local, StringComparison.CurrentCultureIgnoreCase) != 0)
            return false;

        return true;
    }

    public static bool operator !=(PathSegment? left, PathSegment? right)
    {
        return !(left == right);
    }

    public override bool Equals(object? obj)
    {
        PathSegment? right = obj as PathSegment;

        if (obj == null)
            throw new ArgumentException(nameof(obj));

        return this == right;
    }

    public override int GetHashCode() => $"{m_segment}".GetHashCode();
}
