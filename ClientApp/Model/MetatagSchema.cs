using System;
using System.Collections.Generic;
using System.Windows.Markup;
using Emgu.CV.Features2D;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Thetacat.Metatags;
using Thetacat.ServiceClient;
using Thetacat.Standards;
using Thetacat.Types;

namespace Thetacat.Model;

public class MetatagSchema
{
    public enum Standard
    {
        IPTC,
        EXIF,
        JPEG,
        JFIF,
        Nikon1,
        Nikon2,
        XMP,
        User,
        Unknown
    };

    private static readonly Dictionary<Standard, string> m_standardMap =
        new()
        {
            { Standard.IPTC, StandardsMappings.Iptc.Tag },
            { Standard.EXIF, StandardsMappings.Exif.Tag },
            { Standard.JPEG, StandardsMappings.Jpeg.Tag },
            { Standard.JFIF, StandardsMappings.Jfif.Tag },
            { Standard.Nikon1, StandardsMappings.ExifMakernotes_Nikon1.Tag },
            { Standard.Nikon2, StandardsMappings.ExifMakernotes_Nikon2.Tag },
            { Standard.User, "user" },
        };

    public static Standard GetStandardFromString(string standard)
    {
        foreach (Standard key in m_standardMap.Keys)
        {
            if (m_standardMap[key] == standard) 
                return key;
        }

        return Standard.Unknown;
    }

    public static string GetStandardString(Standard standard)
    {
        if (m_standardMap.TryGetValue(standard, out string? s))
            return s;

        throw new Exception($"unknown standard enum ${standard}");
    }

    public List<Metatag> Metatags { get; set; } = new List<Metatag>();
    public int SchemaVersion { get; set; } = 0;

    private MetatagTree? m_tree = null;

    /*----------------------------------------------------------------------------
        %%Function: FindId
        %%Qualified: Thetacat.Model.MetatagSchema.FindId

        Find the given metatag by its id
    ----------------------------------------------------------------------------*/
    public Metatag? FindFirstMatchingItem(IMetatagMatcher<IMetatag> matcher)
    {
        foreach (Metatag metatag in Metatags)
        {
            if (matcher.IsMatch(metatag))
                return metatag;
        }

        return null;
    }

    /*----------------------------------------------------------------------------
        %%Function: FindByName
        %%Qualified: Thetacat.Model.MetatagSchema.FindByName

        Find the first metatag that matches the given name. Search only under
        parent (if given)
    ----------------------------------------------------------------------------*/
    public Metatag? FindByName(Metatag? parent, string name)
    {
        if (parent != null)
        {
            m_tree ??= new MetatagTree(Metatags);

            IMetatagTreeItem? item = m_tree.FindMatchingChild(MetatagTreeItemMatcher.CreateNameMatch(name), -1);

            return item != null ? FindFirstMatchingItem(MetatagMatcher.CreateIdMatch(item.ID)) : null;
        }

        // otherwise, just return the first matching name
        return FindFirstMatchingItem(MetatagMatcher.CreateNameMatch(name));
    }

    public void AddMetatag(Metatag metatag)
    {
        Metatags.Add(metatag);

        if (m_tree != null)
        {
            IMetatagTreeItem newItem = MetatagTreeItem.CreateFromMetatag(metatag);

            if (metatag.Parent == null)
            {
                m_tree.Children.Add(newItem);
            }
            else
            {
                IMetatagTreeItem? parent = m_tree.FindMatchingChild(MetatagTreeItemMatcher.CreateIdMatch(metatag.Parent.Value.ToString()), -1);

                if (parent == null)
                    throw new Exception($"must provide an existing parent ID when adding a metatag: ${metatag}");

                parent.Children.Add(newItem);
            }
        }
    }

    public static MetatagSchema CreateFromService(ServiceMetatagSchema serviceMetatagSchema)
    {
        MetatagSchema schema = new();

        if (serviceMetatagSchema.Metatags != null)
        {
            foreach (ServiceMetatag serviceMetatag in serviceMetatagSchema.Metatags)
            {
                schema.Metatags.Add(Metatag.CreateFromService(serviceMetatag));
            }
        }

        schema.SchemaVersion = serviceMetatagSchema.SchemaVersion ?? 0;
        return schema;
    }
}
