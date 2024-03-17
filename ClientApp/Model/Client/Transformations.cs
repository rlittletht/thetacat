using MetadataExtractor;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Thetacat.Metatags.Model;
using KeyValuePair = System.Collections.Generic.KeyValuePair;

namespace Thetacat.Model.Client;

public class Transformations
{
    public static Guid s_rotateTransform = BuiltinTags.s_TransformRotateID;

    private readonly Dictionary<Guid, string?> m_transformations = new();

    public static Transformations Empty => new Transformations();

    public bool IsEmpty => m_transformations.Count == 0;

    public IReadOnlyDictionary<Guid, string?> _Transformations => m_transformations;

    public Transformations() { }

    public Transformations(IReadOnlyCollection<MediaTag> transformTags)
    {
        foreach (MediaTag tag in transformTags)
        {
            m_transformations.Add(tag.Metatag.ID, tag.Value);
        }
    }

    // need a constructor from a mediaitem or a list of transform tags
    public Transformations(MediaItem item)
    {
        // for now we only have one possible transformation
        int? rotate = item.TransformRotate;

        if (rotate != null)
            m_transformations.Add(s_rotateTransform, rotate.ToString());
    }

    public Transformations(string transformationsKey)
    {
        if (transformationsKey != string.Empty)
        {
            string[] keys = transformationsKey.Split("-");

            foreach (string key in keys)
            {
                string[] pair = key.Split('@');

                string? value = pair.Length > 1 ? pair[1] : null;

                if (Guid.TryParse(pair[0], out Guid id))
                    m_transformations.Add(id, value);
            }
        }
    }

    /*----------------------------------------------------------------------------
        %%Function: TransformationsKey
        %%Qualified: Thetacat.Model.Client.Transformations.TransformationsKey

        Transformation tags stack (with their value), so we need to generate a
        unique key for each combination
    ----------------------------------------------------------------------------*/
    public string TransformationsKey
    {
        get
        {
            List<string> keys = new List<string>();

            foreach (KeyValuePair<Guid, string?> kvp in m_transformations)
            {
                if (kvp.Value != null)
                    keys.Add($"{kvp.Key:N}@{kvp.Value}");
                else
                    keys.Add($"{kvp.Key:N}");
            }

            return string.Join("-", keys);
        }
    }

    public bool IsEqualTransformations(Transformations other)
    {
        if (m_transformations.Count != other.m_transformations.Count)
            return false;

        foreach (KeyValuePair<Guid, string?> kvp in m_transformations)
        {
            if (!other.m_transformations.TryGetValue(kvp.Key, out string? otherValue))
                return false;

            if (kvp.Value == null && otherValue == null)
                continue;

            if (kvp.Value == null || otherValue == null)
                return false;

            if (string.CompareOrdinal(kvp.Value, otherValue) != 0)
                return false;
        }

        return true;
    }

    public bool IsEqualTransformations(string transformationsKey)
    {
        Transformations other = new Transformations(transformationsKey);

        return IsEqualTransformations(other);
    }
}
