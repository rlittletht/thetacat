using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace Thetacat.Model.Metatags;

public class MetatagBuilder
{
    private readonly Metatag m_building;

    public MetatagBuilder(Guid id)
    {
        m_building = new()
        {
            ID = id
        };
    }

    public static MetatagBuilder Create()
    {
        return Create(Guid.NewGuid());
    }

    public static MetatagBuilder Create(Guid id)
    {
        MetatagBuilder builder = new(id);

        return builder;
    }

    public MetatagBuilder SetName(string name)
    {
        m_building.Name = name;
        return this;
    }

    public MetatagBuilder SetParentID(Guid? parentID)
    {
        m_building.Parent = parentID;
        return this;
    }

    public MetatagBuilder SetDescription(string description)
    {
        m_building.Description = description;
        return this;
    }

    public Metatag Build()
    {
        return m_building;
    }
}