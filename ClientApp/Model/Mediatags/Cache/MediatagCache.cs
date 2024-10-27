using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Data.SqlClient;
using TCore.PostfixText;
using TCore.XmlSettings;
using Thetacat.Model;
using Thetacat.Model.Mediatags;
using Thetacat.ServiceClient;
using Thetacat.Types;
using XMLIO;
using static Thetacat.Export.BackupDatabase;

namespace Thetacat.Model.Mediatags.Cache;

public class MediatagCache : IEnumerable<ServiceMediaTag>
{
    private readonly Dictionary<Guid, List<ServiceMediaTag>> m_tags = new();

    private readonly string m_cacheFilepath;

    private int m_tagClock;
    private int m_resetClock;
    private Guid m_catalogId;

    private bool m_dirty = false;

    private static string s_rootElement = "mediatagCache";

    private static string s_attr_tagClock = "tagClock";
    private static string s_attr_resetClock = "resetClock";

    public static string s_uri = "https://schemas.thetasoft.com/thetacat/metatagCache/2024";

    public MediatagCache(Guid? catalogID = null)
    {
        m_catalogId = catalogID ?? App.State.ActiveProfile.CatalogID;

        m_cacheFilepath = Path.Combine(App.State.ActiveProfile.RootForCatalogCache(m_catalogId), "metatag-cache.xml");
    }

#region IEnumerable

    public IEnumerator<ServiceMediaTag> GetEnumerator()
    {
        foreach (KeyValuePair<Guid, List<ServiceMediaTag>> pair in m_tags)
        {
            foreach (ServiceMediaTag tag in pair.Value)
            {
                yield return tag;
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

#endregion

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
    public void WriteCache(bool fWriteAlways)
    {
        if (!fWriteAlways && !m_dirty)
            return;

        XmlWriterSettings settings = new XmlWriterSettings();
        settings.Async = true;

        // first, make a backup

        string backup = $"{m_cacheFilepath}_{Guid.NewGuid()}.bak";

        try
        {
            File.Copy(m_cacheFilepath, backup);
        }
        catch (FileNotFoundException)
        {}

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
    public void ReadFromFile()
    {
        try
        {
            using Stream stm = File.Open(m_cacheFilepath, FileMode.Open, FileAccess.Read);
            using XmlReader reader = XmlReader.Create(stm);

            // check for empty
            if (!XmlIO.Read(reader))
                return;

            XmlIO.SkipNonContent(reader);

            if (!ReadMediatagCache(reader))
                throw new CatException("Failed to read mediatag cache");
        }
        catch (FileNotFoundException)
        {
            // just an empty tag cache...
        }
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

    void UpdateTag(ServiceMediaTag tag)
    {
        if (!m_tags.ContainsKey(tag.MediaId))
        {
            if (tag.Deleted)
                // nothing to do if we don't have this media item and the tag is deleted...
                return;

            m_tags.Add(tag.MediaId, new List<ServiceMediaTag>());
        }

        if (tag.Deleted)
        {
            if (m_tags[tag.MediaId].RemoveAll((_tag) => _tag.Id == tag.Id) > 0)
                m_dirty = true;

            return;
        }

        bool found = false;

        foreach (ServiceMediaTag _tag in m_tags[tag.MediaId])
        {
            if (_tag.Id == tag.Id)
            {
                _tag.Value = tag.Value;
                _tag.Clock = tag.Clock;
                found = true;
                m_dirty = true;
            }
        }

        if (!found)
        {
            m_tags[tag.MediaId].Add(tag);
            m_dirty = true;
        }
    }

    /*----------------------------------------------------------------------------
        %%Function: ReadFullCatalogMediaTagsWithCache
        %%Qualified: Thetacat.Model.Mediatags.Cache.MediatagCache.ReadFullCatalogMediaTagsWithCache

        Read the local mediatag cache for this catalog, then check for updates
        on the server:

        - if the reset-clock is less than the servers reset clock, throw everything
          away and read the full set of tags. stop.
        - query all of the items from the server whose clock is greater than the
          the tag clock in the local cache

            - for any items marked as deleted from the server, delete those items
              from the mediatags we just read in
    ----------------------------------------------------------------------------*/
    public void ReadFullCatalogMediaTagsWithCache()
    {
        // first, read the cache from disk
        ReadFromFile();

        // now query all the items from the database with a later tag clock

        ServiceMediaTagsWithClocks tagsWithClocks = ServiceInterop.ReadMediaTagsForClock(m_catalogId, m_tagClock + 1);

        if (m_resetClock != tagsWithClocks.ResetClock)
        {
            // we have been instructed to reset our cache. get everything except clock == 0
            m_tags.Clear();
            tagsWithClocks = ServiceInterop.ReadMediaTagsForClock(m_catalogId, 1);
        }

        // and merge the new items into what we had cached

        foreach (ServiceMediaTag tag in tagsWithClocks.Tags)
        {
            UpdateTag(tag);
        }

        // update our clocks
        m_resetClock = tagsWithClocks.ResetClock;
        m_tagClock = tagsWithClocks.TagClock;
    }

    /*----------------------------------------------------------------------------
        %%Function: UpdateMediatagsWithNoClockAndincrementVectorClock
        %%Qualified: Thetacat.Model.Mediatags.Cache.MediatagCache.UpdateMediatagsWithNoClockAndincrementVectorClock
    ----------------------------------------------------------------------------*/
    public static void UpdateMediatagsWithNoClockAndincrementVectorClock()
    {
        int retries = 5;

        while (retries-- > 0)
        {
            try
            {
                ServiceInterop.UpdateMediatagsWithNoClockAndincrementVectorClock(App.State.ActiveProfile.CatalogID);
                return;
            }
            catch (SqlException exc)
            {
                if (exc.Message != "Coherency Failure")
                    throw;

                // fallthrough to retry
            }
        }

        throw new CatException("Could not update mediatags clock after 5 coherency failure retries");
    }
}
