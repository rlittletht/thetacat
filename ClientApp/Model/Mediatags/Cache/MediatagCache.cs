using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using TCore.PostfixText;
using TCore.XmlSettings;
using Thetacat.Model;
using Thetacat.Model.Mediatags;
using Thetacat.ServiceClient;
using Thetacat.Types;
using XMLIO;
using static Thetacat.Export.BackupDatabase;

namespace Thetacat.Model.Mediatags.Cache;

public class MediatagCache
{
    private readonly Dictionary<Guid, List<ServiceMediaTag>> m_tags = new();

    private readonly string m_cacheFilepath = Path.Combine(App.State.ActiveProfile.RootForCatalogCache(), "metatag-cache.xml");

    private int m_tagClock;
    private int m_resetClock;

    private static string s_rootElement = "mediatagCache";

    private static string s_attr_tagClock = "tagClock";
    private static string s_attr_resetClock = "resetClock";

    public static string s_uri = "https://schemas.thetasoft.com/thetacat/metatagCache/2024";

#region Write

    /*----------------------------------------------------------------------------
        %%Function: CreateFromCatalog
        %%Qualified: Thetacat.Model.Mediatags.Cache.MediatagCache.Create
    ----------------------------------------------------------------------------*/
    public static MediatagCache CreateFromCatalog(ICatalog catalog, int tagClock, int resetClock)
    {
        MediatagCache cache = new MediatagCache();

        foreach (MediaItem item in catalog.GetMediaCollection())
        {
            foreach (MediaTag tag in item.MediaTags)
            {
                ServiceMediaTag serviceTag =
                    new ServiceMediaTag()
                    {
                        Id = tag.Metatag.ID,
                        MediaId = item.ID,
                        Value = tag.Value
                    };

                if (!cache.m_tags.ContainsKey(serviceTag.MediaId))
                    cache.m_tags.Add(serviceTag.MediaId, new List<ServiceMediaTag>());

                cache.m_tags[serviceTag.MediaId].Add(serviceTag);
            }
        }

        cache.m_tagClock = tagClock;
        cache.m_resetClock = resetClock;

        return cache;
    }

    public delegate void WriteChildrenDelegate(XmlWriter writer);

    /*----------------------------------------------------------------------------
        %%Function: WriteElement
        %%Qualified: Thetacat.Model.Mediatags.Cache.MediatagCache.WriteElement

        This really should be a static function on XmlWriter...
    ----------------------------------------------------------------------------*/
    public static void WriteElement(XmlWriter writer, string element, WriteChildrenDelegate? children)
    {
        if (children != null)
        {
            writer.WriteStartElement(element, s_uri);
            children(writer);
            writer.WriteEndElement();
        }
        else
        {
            writer.WriteElementString(element, s_uri);
        }
    }

    /*----------------------------------------------------------------------------
        %%Function: WriteMediatags
        %%Qualified: Thetacat.Model.Mediatags.Cache.MediatagCache.WriteMediatags
    ----------------------------------------------------------------------------*/
    public static void WriteMediatags(XmlWriter writer, IReadOnlyCollection<ServiceMediaTag> tags)
    {
        foreach (ServiceMediaTag tag in tags)
        {
            MediatagCacheItem.Write(writer, tag);
        }
    }

    /*----------------------------------------------------------------------------
        %%Function: WriteMediatagsByMediaId
        %%Qualified: Thetacat.Model.Mediatags.Cache.MediatagCache.WriteMediatagsByMediaId
    ----------------------------------------------------------------------------*/
    public static void WriteMediatagsByMediaId(XmlWriter writer, Dictionary<Guid, List<ServiceMediaTag>> tags)
    {
        foreach (Guid id in tags.Keys)
        {
            MediatagsCacheItem.Write(writer, id, tags[id]);
        }
    }

    /*----------------------------------------------------------------------------
        %%Function: WriteCache
        %%Qualified: Thetacat.Model.Mediatags.Cache.MediatagCache.WriteCache
    ----------------------------------------------------------------------------*/
    public void WriteCache()
    {
        XmlWriterSettings settings = new XmlWriterSettings();
        settings.Async = true;

        using XmlWriter writer = XmlWriter.Create(m_cacheFilepath);

        WriteElement(
            writer,
            s_rootElement,
            (_writer) =>
            {
                _writer.WriteAttributeString(s_attr_tagClock, m_tagClock.ToString());
                _writer.WriteAttributeString(s_attr_resetClock, m_resetClock.ToString());

                WriteMediatagsByMediaId(_writer, m_tags);
            });
    }

#endregion

#region Read

    /*----------------------------------------------------------------------------
        %%Function: CreateFromFile
        %%Qualified: Thetacat.Model.Mediatags.Cache.MediatagCache.CreateFromFile
    ----------------------------------------------------------------------------*/
    public static MediatagCache CreateFromFile()
    {
        MediatagCache cache = new MediatagCache();

        using Stream stm = File.Open(cache.m_cacheFilepath, FileMode.Open, FileAccess.Read);
        using XmlReader reader = XmlReader.Create(stm);

        // check for empty
        if (!XmlIO.Read(reader))
            return cache;

        XmlIO.SkipNonContent(reader);

        if (!cache.ReadMediatagCache(reader))
            throw new CatException("Failed to read mediatag cache");

        return cache;
    }

    /*----------------------------------------------------------------------------
        %%Function: FParseAttributes
        %%Qualified: Thetacat.Model.Mediatags.Cache.MediatagCache.FParseAttributes
    ----------------------------------------------------------------------------*/
    static bool FParseAttributes(string attribute, string value, MediatagCache cache)
    {
        if (attribute == s_attr_tagClock)
        {
            cache.m_tagClock = int.Parse(value);
            return true;
        }

        if (attribute == s_attr_resetClock)
        {
            cache.m_resetClock = int.Parse(value);
            return true;
        }

        throw new XmlioExceptionSchemaFailure($"unknown attribute {attribute}: {value}");
    }

    /*----------------------------------------------------------------------------
        %%Function: FParseElements
        %%Qualified: Thetacat.Model.Mediatags.Cache.MediatagCache.FParseElements
    ----------------------------------------------------------------------------*/
    static bool FParseElements(XmlReader reader, string element, MediatagCache cache)
    {
        if (element == MediatagsCacheItem.s_rootElement)
        {
            MediatagsCacheItem item = MediatagsCacheItem.CreateFromReader(reader);

            cache.m_tags.Add(item.MediaId, item.Tags);
            return true;
        }

        throw new XmlioExceptionSchemaFailure($"unknown element {element}");
    }

    /*----------------------------------------------------------------------------
        %%Function: ReadMediatagCache
        %%Qualified: Thetacat.Model.Mediatags.Cache.MediatagCache.ReadMediatagCache
    ----------------------------------------------------------------------------*/
    bool ReadMediatagCache(XmlReader reader)
    {
        return XmlIO.FReadElement(reader, this, s_rootElement, FParseAttributes, FParseElements);
    }

#endregion
}
