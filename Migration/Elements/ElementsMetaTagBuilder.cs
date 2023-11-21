using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace Thetacat.Migration.Elements;

public class ElementsMetaTagBuilder
{
    private ElementsMetaTag m_building;

    public static ElementsMetaTagBuilder Create()
    {
        ElementsMetaTagBuilder builder = new();

        builder.m_building = new ElementsMetaTag();

        return builder;
    }

    public ElementsMetaTagBuilder SetName(string name)
    {
        m_building.Name = name;
        return this;
    }

    public ElementsMetaTagBuilder SetID(string id)
    {
        m_building.ID = id;
        return this;
    }

    public ElementsMetaTagBuilder SetParentID(string parentID)
    {
        m_building.ParentID = parentID;
        return this;
    }

    public ElementsMetaTagBuilder SetParentName(string parentName)
    {
        m_building.ParentName = parentName;
        return this;
    }

    public ElementsMetaTagBuilder SetElementsTypeName(string typeName)
    {
        m_building.ElementsTypeName = typeName;
        return this;
    }

    public ElementsMetaTag Build()
    {
        return m_building;
    }
}