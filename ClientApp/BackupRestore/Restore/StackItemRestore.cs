using System;
using System.Xml;
using XMLIO;

namespace Thetacat.BackupRestore.Restore;

public class StackItemRestore
{
    public Guid MediaId;
    public int StackIndex;

    public static bool FParseAttribute(string attribute, string value, StackItemRestore itemRestore)
    {
        switch (attribute)
        {
            case "mediaId":
                if (Guid.TryParse(value, out Guid id))
                {
                    itemRestore.MediaId = id;
                    return true;
                }

                return false;
            case "stackIndex":
                if (Int32.TryParse(value, out Int32 numValue))
                {
                    itemRestore.StackIndex = numValue;
                    return true;
                }

                return false;
        }

        return false;
    }

    public StackItemRestore(XmlReader reader)
    {
        XmlIO.FReadElement(reader, this, "stackItem", FParseAttribute, null);
    }
}
