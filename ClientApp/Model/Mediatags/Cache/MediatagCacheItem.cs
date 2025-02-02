﻿using System;
using System.Threading;
using System.Xml;
using Thetacat.Metatags.Model;
using Thetacat.ServiceClient;
using XMLIO;

namespace Thetacat.Model.Mediatags.Cache;

public class MediatagCacheItem
{
    public static string s_rootElement = "tag";

    public static string s_attr_MediaId = "mediaId";
    public static string s_attr_Id = "id";
    public static string s_attr_Deleted = "deleted";

    public ServiceMediaTag MediaTag => m_creating;

    /*----------------------------------------------------------------------------
        %%Function: Write
        %%Qualified: Thetacat.Model.Mediatags.Cache.MediatagCacheItem.Write
    ----------------------------------------------------------------------------*/
    public static void Write(XmlWriter writer, ServiceMediaTag tag)
    {
        MediatagCache.WriteElement(
            writer,
            s_rootElement,
            (_writer) =>
            {
                _writer.WriteAttributeString(s_attr_Id, tag.Id.ToString());
                if (tag.Deleted)
                    writer.WriteAttributeString(s_attr_Deleted, "true");

                if (tag.Value != null)
                {
                    if (string.IsNullOrWhiteSpace(tag.Value))
                        writer.WriteAttributeString("xml", "space", null, "preserve");

                    _writer.WriteString(tag.Value);
                }
            });
    }

    private ServiceMediaTag m_creating = new ServiceMediaTag();

    /*----------------------------------------------------------------------------
        %%Function: FParseAttributes
        %%Qualified: Thetacat.Model.Mediatags.Cache.MediatagCacheItem.FParseAttributes
    ----------------------------------------------------------------------------*/
    static bool FParseAttributes(string attribute, string value, MediatagCacheItem item)
    {
        if (attribute == s_attr_MediaId)
        {
            item.m_creating.MediaId = Guid.Parse(value);
            return true;
        }

        if (attribute == s_attr_Id)
        {
            item.m_creating.Id = Guid.Parse(value);
            return true;
        }

        if (attribute == s_attr_Deleted)
        {
            if (value == "true")
                item.m_creating.Deleted = true;

            return true;
        }

        throw new XmlioExceptionSchemaFailure($"unknown attribute {attribute}: {value}");
    }

    /*----------------------------------------------------------------------------
        %%Function: CreateFromReader
        %%Qualified: Thetacat.Model.Mediatags.Cache.MediatagCacheItem.CreateFromReader
    ----------------------------------------------------------------------------*/
    public static MediatagCacheItem CreateFromReader(XmlReader reader, Guid mediaId)
    {
        MediatagCacheItem item = new MediatagCacheItem();
        XmlIO.ContentCollector collector = new XmlIO.ContentCollector();

        XmlIO.FReadElement(reader, item, s_rootElement, FParseAttributes, null, collector);

        item.m_creating.Value = collector.NullContent ? null : collector.ToString();
        item.MediaTag.MediaId = mediaId;

        return item;
    }
}
