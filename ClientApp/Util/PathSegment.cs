using System;
using System.IO;
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
            string pathLocal = path.Local;
            if (pathLocal.StartsWith(".\\"))
                pathLocal = pathLocal.Substring(2);

            a = new PathSegment(Path.Join(a.Local, pathLocal));
        }

        return a;
    }

    public static PathSegment Join(string a, params string[] paths)
    {
        PathSegment segment = new PathSegment(a);

        foreach (string path in paths)
        {
            if (path == "." || path == ".\\")
                continue;  // meaningless join

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

    public static PathSegment GetPathDirectory(PathSegment path)
    {
        return CreateFromString(Path.GetDirectoryName(path));
    }

    public PathSegment GetPathDirectory()
    {
        return GetPathDirectory(this);
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

    public static PathSegment GetFilename(PathSegment path)
    {
        return new PathSegment(Path.GetFileName(path.Local));
    }

    public PathSegment? GetFilename()
    {
        return GetFilename(this);
    }

    public PathSegment? GetLeafItem()
    {
        int ichLim = m_segment.Length - 1;
        int ich = m_segment.LastIndexOf('/', ichLim - 1);
        if (ich == -1)
            return null;

        return new PathSegment(m_segment[(ich + 1)..(ichLim + 1)]);
    }

    /*----------------------------------------------------------------------------
        %%Function: DoesPathSubsumePath
        %%Qualified: Thetacat.Util.PathSegment.DoesPathSubsumePath

        Full path subsumes the check if check is inside full path
    ----------------------------------------------------------------------------*/
    public static bool DoesPathSubsumePath(PathSegment fullPath, PathSegment checkPath)
    {
        string full = fullPath;
        string check = checkPath;

        if (string.Compare(full, check, StringComparison.InvariantCultureIgnoreCase) == 0)
            return true;

        if (!check.EndsWith('/'))
            check = $"{check}/";

        if (full.StartsWith(check, StringComparison.CurrentCultureIgnoreCase))
            return true;

        return false;
    }

    public bool Subsumes(PathSegment path)
    {
        return DoesPathSubsumePath(this, path);
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

    public override int GetHashCode() => $"{m_segment.ToUpper()}".GetHashCode();
}
