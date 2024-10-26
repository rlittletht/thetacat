using System;
using System.Collections.Generic;
using System.Xml;
using Thetacat.ServiceClient;
using XMLIO;

namespace Thetacat.Model.Mediatags.Cache;

public class MediatagsCacheItem
{
    public static string s_rootElement = "mediatags";

    public static string s_attr_MediaId = "mediaId";

    public List<ServiceMediaTag> Tags => m_creating;
    public Guid MediaId => m_guidCreating;

    private List<ServiceMediaTag> m_creating = new();

    private Guid m_guidCreating;

    /*----------------------------------------------------------------------------
        %%Function: Write
        %%Qualified: Thetacat.Model.Mediatags.Cache.MediatagsCacheItem.Write
    ----------------------------------------------------------------------------*/
    public static void Write(XmlWriter writer, Guid mediaId, IReadOnlyCollection<ServiceMediaTag> tags)
    {
        MediatagCache.WriteElement(
            writer,
            s_rootElement,
            (_writer) =>
            {
                _writer.WriteAttributeString("mediaId", mediaId.ToString());

                foreach (ServiceMediaTag tag in tags)
                {
                    MediatagCacheItem.Write(_writer, tag);
                }
            });
    }

    /*----------------------------------------------------------------------------
        %%Function: FParseAttributes
        %%Qualified: Thetacat.Model.Mediatags.Cache.MediatagsCacheItem.FParseAttributes
    ----------------------------------------------------------------------------*/
    static bool FParseAttributes(string attribute, string value, MediatagsCacheItem item)
    {
        if (attribute == s_attr_MediaId)
        {
            item.m_guidCreating = Guid.Parse(value);
            return true;
        }

        throw new XmlioExceptionSchemaFailure($"unknown attribute {attribute}: {value}");
    }

    /*----------------------------------------------------------------------------
        %%Function: FParseElements
        %%Qualified: Thetacat.Model.Mediatags.Cache.MediatagsCacheItem.FParseElements
    ----------------------------------------------------------------------------*/
    static bool FParseElements(XmlReader reader, string element, MediatagsCacheItem item)
    {
        if (element == MediatagCacheItem.s_rootElement)
        {
            item.m_creating.Add(MediatagCacheItem.CreateFromReader(reader).MediaTag);
            return true;
        }

        throw new XmlioExceptionSchemaFailure($"unknown element {element}");
    }

    /*----------------------------------------------------------------------------
        %%Function: CreateFromReader
        %%Qualified: Thetacat.Model.Mediatags.Cache.MediatagsCacheItem.CreateFromReader
    ----------------------------------------------------------------------------*/
    public static MediatagsCacheItem CreateFromReader(XmlReader reader)
    {
        MediatagsCacheItem item = new MediatagsCacheItem();

        XmlIO.FReadElement(reader, item, s_rootElement, FParseAttributes, FParseElements);

        return item;
    }
}
