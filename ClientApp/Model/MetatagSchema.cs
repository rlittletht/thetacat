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

    void AddMetatagNoValidation(Metatag metatag)
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

    /*----------------------------------------------------------------------------
        %%Function: AddMetatag
        %%Qualified: Thetacat.Model.MetatagSchema.AddMetatag

        This is the most core AddMetatag. It requires that you have a parent
        set. You CANNOT add a root standard tag with this function
    ----------------------------------------------------------------------------*/
    public void AddMetatag(Metatag metatag)
    {
        if (metatag.Parent == null)
            throw new ArgumentException("must specify parent for metatag");

        AddMetatagNoValidation(metatag);
    }

    /*----------------------------------------------------------------------------
        %%Function: AddStandardRoot
        %%Qualified: Thetacat.Model.MetatagSchema.AddStandardRoot

        This is really the same as AddMetatag, but it allows null for the parent

    ----------------------------------------------------------------------------*/
    public void AddStandardRoot(Metatag metatag)
    {
        AddMetatagNoValidation(metatag);
    }

    /*----------------------------------------------------------------------------
        %%Function: AddNewStandardRoot
        %%Qualified: Thetacat.Model.MetatagSchema.AddNewStandardRoot

        This will create a new standard root metatag and add it (and return it)
    ----------------------------------------------------------------------------*/
    public Metatag AddNewStandardRoot(MetatagStandards.Standard standard)
    {
        StandardMappings mappings = MetatagStandards.GetStandardMappings(standard);

        Metatag metatag = MetatagBuilder
           .Create()
           .SetName(mappings.Tag)
           .SetDescription($"{mappings.Tag} root")
           .Build();

        AddMetatagNoValidation(metatag);

        return metatag;
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
