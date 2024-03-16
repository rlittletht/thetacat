using System;
using System.Collections.Generic;
using System.Xml;
using Thetacat.Model;
using Thetacat.ServiceClient;
using XMLIO;

namespace Thetacat.BackupRestore.Restore;

public class StacksRestore
{
    public MediaStackType StackType;
    public List<ServiceStack> Stacks = new();
    private ServiceStack StackBuilding = new();


    static bool FParseStackItem(XmlReader reader, string element, StacksRestore stacksRestore)
    {
        if (element != "stackItem")
            return false;

        StackItemRestore item = new StackItemRestore(reader);

        stacksRestore.StackBuilding.StackItems!.Add(
            new ServiceStackItem()
            {
                MediaId = item.MediaId,
                OrderHint = item.StackIndex
            });

        return true;
    }

    static bool FParseStack(XmlReader reader, string element, StacksRestore stacksRestore)
    {
        if (element != "stack")
            return false;

        stacksRestore.StackBuilding =
            new ServiceStack
            {
                StackItems = new List<ServiceStackItem>(),
                StackType = stacksRestore.StackType
            };

        if (!XmlIO.FReadElement(reader, stacksRestore, "stack", FParseAttribute, FParseStackItem))
            return false;

        // make sure there's a description even if we didn't read one
        stacksRestore.StackBuilding.Description ??= string.Empty;
        stacksRestore.Stacks.Add(stacksRestore.StackBuilding);

        return true;
    }

    public static bool FParseAttribute(string attribute, string value, StacksRestore stacksRestore)
    {
        switch (attribute)
        {
            case "id":
                if (Guid.TryParse(value, out Guid id))
                {
                    stacksRestore.StackBuilding.Id = id;
                    return true;
                }

                return false;
            case "description":
                stacksRestore.StackBuilding.Description = value;
                return true;
        }

        return false;
    }

    public StacksRestore(XmlReader reader, string rootElement, MediaStackType stackType)
    {
        StackType = stackType;
        XmlIO.FReadElement(reader, this, rootElement, null, FParseStack);
    }
}
