using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Globalization;
using System.Windows;
using TCore;
using Thetacat.Migration.Elements;
using Thetacat.Migration.Elements.Media;
using Thetacat.Migration.Elements.Metadata.UI;
using Thetacat.Migration.Elements.Versions;
using Thetacat.TCore.TcSqlLite;
using Thetacat.Types;

namespace Thetacat.ServiceClient.LocalDatabase;

/*
NOTES on Elements database

The tables are an indulgence of abstractions. Lots of reuse of names, so I will assign [KEY] names that identify
names across tables (so you can relate them)

media_table
    holds all the actual media items. ID is the unique id for the media [MEDIA_ID]

tag_table
    defines all of the metatags available. ID is the unqique ID for each tag definition [TAG_ID].
    these are stored hiearchically, so Parent ID is the [TAG_ID] of the parent tag (or 0 for no parent)

    each row defines a single tag

metadata_blob_table
metadata_decimal_table
metadata_integer_table
metadata_string_table
    stores the value of a single metadata/value pair, indexed by [METADATA_ID]. Each table stores only its
    respective type (so real numbers in *_decimal_table, whole numbers in *_integer_table, etc). 

    any time you see something point to [METADATA_ID], it means it is referring to (metadata/value) pair.
    description_id [DESCRIPTION_ID] uniquely identifies the NAME of the metadata

metadata_description_table
    stores a description of a single metadata item (not a value). ID [DESCRIPTION_ID] is the unique identifier for
    each metadata item 

media_to_metadata_table
    maps [MEDIA_ID] to [METADATA_ID]. All metadata values can be fetched this way

tag_to_media_table
    maps [TAG_ID] to [MEDIA_ID], associating one or more metatags (from tag_table) with a media item. this is how
    metatags are 'painted' onto media. (i.e. the tag for "2019Vacation" defined in tag_table with [TAG_ID]=100
    might be associated with 2 pictures with [MEDIA_ID] = 10 and 20 as:
        [TAG_ID]:100, [MEDIA_ID]:10
        [TAG_ID]:100, [MEDIA_ID]:20

tag_to_metadata_table
    maps [TAG_ID] to [METADATA_ID], associeated one ore more metadata values with a metatag TAG. This is for things
    like metadata ABOUT a tag. For example, tag "2019Vacation" could have metadata like a note "Vacation to Hawaii in 2019".
    (also things like icons for a TAG, or import details if the TAG is for a particular import

// these are versions of the same picture
version_stack_to_media_table
    maps [STACK_TAG_ID] to [MEDIA_ID], associateing one stack with one or more media items. [MEDIA_INDEX] is the
    location in the stack, with 0 being the "top" of the stack (most recent). [PARENT_ID] is the MEDIA_ID of the
    item below this item (the largest index has 0 for a parent)

// these are just stacked together (presumably by the user)
media_stack_to_media_table
    maps [STACK_TAG_ID] to [MEDIA_ID], associateing one stack with one or more media items. [MEDIA_INDEX] is the
    location in the stack, with 0 being the "top" of the stack (most recent). [PARENT_ID] is the MEDIA_ID of the
    item below this item (the largest index has 0 for a parent)

// IMPORT DATE
// Elements creates a tag for each import that happens. These tags are of type "import", and they are named
// "import YYYY-MM-DDThh:mm:ss".  They appear to be in UTC. So to figure out when media was imported, you have
// to find the import tag that was applied to it

useful queries:

    Show all the string-type metadata values for all of the media in the library. One row per metadata / media pair.

    SELECT MMT.media_id, MT.full_filepath, MST.value, MDT.identifier from media_to_metadata_table AS MMT
    INNER JOIN media_table MT on MT.id = MMT.media_id
    INNER JOIN metadata_string_table MST on MST.id = MMT.metadata_id
    INNER JOIN metadata_description_table MDT on MDT.id = MST.description_id    
 */
public class ElementsDb
{
    private ISql? m_connection;

    private ISql _Connection => m_connection ?? throw new CatExceptionInitializationFailure("ElementsDb not properly created");

    private static Dictionary<string, string> s_aliases =
        new()
        {
            { "version_stack_to_media_table", "VS" },
            { "media_stack_to_media_table", "MS" },
            { "tag_to_metadata_table", "TMD" },
            { "tag_table", "TT" },
            { "metadata_description_table", "MDT" },
            { "metadata_string_table", "MST" },
            { "tag_to_media_table", "TMT" },
            { "media_table", "MT" }
        };

    private static readonly string s_queryMediaStacks = @"
        SELECT 
            $$media_stack_to_media_table$$.media_id, $$media_stack_to_media_table$$.stack_tag_id, 
            $$media_stack_to_media_table$$.media_index
        FROM $$#media_stack_to_media_table$$";

    private static readonly string s_queryVersionStacks = @"
        SELECT 
            $$version_stack_to_media_table$$.media_id, $$version_stack_to_media_table$$.stack_tag_id, 
            $$version_stack_to_media_table$$.media_index
        FROM $$#version_stack_to_media_table$$";

    static readonly string s_queryMetatagDefinitions = @"
        SELECT $$tag_table$$.id, $$tag_table$$.name, $$tag_table$$.parent_id, $$tag_table$$.type_name, INR.name AS ParentName
          FROM $$#tag_table$$
            INNER JOIN tag_table as INR 
              ON INR.id = $$tag_table$$.parent_id 
         WHERE $$tag_table$$.type_name NOT LIKE 'history%' AND $$tag_table$$.type_name not like 'import%' AND $$tag_table$$.type_name <> 'version_stack'";

    private static readonly string s_queryMetadataDefinitions = @"
        SELECT $$metadata_description_table$$.id, $$metadata_description_table$$.identifier, $$metadata_description_table$$.data_type
          FROM $$#metadata_description_table$$";

    private readonly string s_queryTagNotes = @"
        SELECT $$tag_table$$.ID, $$metadata_string_table$$.value
          FROM $$#tag_table$$
            INNER JOIN $$#tag_to_metadata_table$$ ON $$tag_to_metadata_table$$.tag_id = $$tag_table$$.ID
            INNER JOIN $$#metadata_string_table$$ ON $$metadata_string_table$$.id = $$tag_to_metadata_table$$.metadata_id
            INNER JOIN $$#metadata_description_table$$ ON $$metadata_description_table$$.id = $$metadata_string_table$$.description_id
          WHERE $$metadata_description_table$$.identifier = 'pse:TagNotes' AND $$metadata_string_table$$.value <> ''";

    public static ElementsDb Create(string databaseFile)
    {
        ElementsDb db =
            new()
            {
                m_connection = SQLite.OpenConnection($"Data Source={databaseFile}")
            };

        return db;
    }

    public void Close()
    {
        m_connection?.Close();
    }

    public PseMetadataSchema ReadMetadataSchema()
    {
        try
        {
            List<PseMetadata> tags =
                _Connection.DoGenericQueryDelegateRead(
                    Guid.NewGuid(),
                    s_queryMetadataDefinitions,
                    s_aliases,
                    (ISqlReader reader, Guid crids, ref List<PseMetadata> building) =>
                    {
                        building.Add(
                            PseMetadataBuilder
                               .Create()
                               .SetPseId(reader.GetInt32(0))
                               .SetPseIdentifier(reader.GetString(1))
                               .SetPseDatatype(reader.GetString(2))
                               .Build());
                    });
            return new PseMetadataSchema(tags);
        }
        catch (TcSqlExceptionNoResults)
        {
            return new PseMetadataSchema(new List<PseMetadata>());
        }
    }

    Dictionary<int, PseMetatag> ReadMetatagDictionary()
    {
        try
        {
            return
                _Connection.DoGenericQueryDelegateRead(
                    Guid.NewGuid(),
                    s_queryMetatagDefinitions,
                    s_aliases,
                    (ISqlReader reader, Guid crids, ref Dictionary<int, PseMetatag> building) =>
                    {
                        int pseId = reader.GetInt32(0);

                        building.Add(
                            pseId,
                            PseMetatagBuilder
                               .Create()
                               .SetID(pseId)
                               .SetName(reader.GetString(1))
                               .SetParentID(reader.GetInt32(2))
                               .SetElementsTypeName(reader.GetString(3))
                               .SetParentName(reader.GetString(4))
                               .Build());
                    });
        }
        catch (TcSqlExceptionNoResults)
        {
            return new Dictionary<int, PseMetatag>();
        }
    }

    void ReadDescriptionsForMetatags(Dictionary<int, PseMetatag> tags)
    {
        SqlSelect selectTags = new SqlSelect();

        selectTags.AddBase(s_queryTagNotes);
        selectTags.AddAliases(s_aliases);

        string sQuery = selectTags.ToString();

        ISqlReader? sqlr = null;
        ISqlCommand cmd = _Connection.CreateCommand();
        cmd.CommandText = sQuery;

        try
        {
            sqlr = cmd.ExecuteReader();

            while (sqlr.Read())
            {
                int pseId = sqlr.GetInt32(0);
                string description = sqlr.GetString(1);

                if (tags.TryGetValue(pseId, out PseMetatag? tag))
                    tag.Description = description;
            }
        }
        finally
        {
            cmd.Close();
            sqlr?.Close();
        }
    }

    /*----------------------------------------------------------------------------
        %%Function: ReadMetadataTags
        %%Qualified: Thetacat.Migration.Elements.ElementsDb.ReadMetadataTags

        This reads all the user defined/custom metatags that can be 'painted'
        on the media items
    ----------------------------------------------------------------------------*/
    public IEnumerable<PseMetatag> ReadMetadataTags()
    {
        Dictionary<int, PseMetatag> tags = ReadMetatagDictionary();
        ReadDescriptionsForMetatags(tags);

        return tags.Values;
    }

    private static readonly string[] s_ignoreMetadata =
    {
        "'pse::FaceDectectorBreezePath'",
        "'pse::FaceDectectorVersion'",
        "'pse::FaceRecognizerVersion'",
        "'xmp:Rating'",
        "'xmpDM:Duration'",
        "'pse::VisualSimilarityIndexed'",
        "'pse:FaceData'",
        "'pse:FileSize'",
        "'pse:FileSizeOriginal'",
        "'pse:TagIconMediaId'",
        "'pre:ca:xmpPath'",
        "'pse:ImportSourceType'",
        "'pse:PrinterName'",
        "'pse:TagIconMediaCropRect'",
        "'pse:albumStyleXmlPath'",
        "'pse:guid'",
//        "'pse:FileDate'",
//        "'pse:FileDateOriginal'",
        "'pse:TagDate'",
        "'xmp:CreateDate'"
    };

    private static readonly string s_queryReadMediaTagValues = $@"
        SELECT MMT.media_id, MST.value, MDT.identifier from media_to_metadata_table AS MMT
            INNER JOIN media_table MT on MT.id = MMT.media_id
            INNER JOIN metadata_string_table MST on MST.id = MMT.metadata_id
            INNER JOIN metadata_description_table MDT on MDT.id = MST.description_id
        WHERE MDT.identifier not in ({string.Join(",", s_ignoreMetadata)})
        UNION 
        SELECT MMT.media_id, MST.value, MDT.identifier from media_to_metadata_table AS MMT
            INNER JOIN media_table MT on MT.id = MMT.media_id
            INNER JOIN metadata_integer_table MST on MST.id = MMT.metadata_id
            INNER JOIN metadata_description_table MDT on MDT.id = MST.description_id
        WHERE MDT.identifier not in ({string.Join(",", s_ignoreMetadata)})
        UNION 
            SELECT MMT.media_id, MST.value, MDT.identifier from media_to_metadata_table AS MMT
            INNER JOIN media_table MT on MT.id = MMT.media_id
            INNER JOIN metadata_decimal_table MST on MST.id = MMT.metadata_id
            INNER JOIN metadata_description_table MDT on MDT.id = MST.description_id
        UNION 
            SELECT MMT.media_id, MST.value, MDT.identifier from media_to_metadata_table AS MMT
            INNER JOIN media_table MT on MT.id = MMT.media_id
            INNER JOIN metadata_date_time_table MST on MST.id = MMT.metadata_id
            INNER JOIN metadata_description_table MDT on MDT.id = MST.description_id
        WHERE MDT.identifier not in ({string.Join(",", s_ignoreMetadata)})";

    void ReadMediaTagValues(Dictionary<int, PseMediaItem> items)
    {
        SqlSelect selectTags = new SqlSelect();

        selectTags.AddBase(s_queryReadMediaTagValues);
        selectTags.AddAliases(s_aliases);

        string sQuery = selectTags.ToString();

        ISqlReader? sqlr = null;
        ISqlCommand cmd = _Connection.CreateCommand();
        cmd.CommandText = sQuery;

        try
        {
            sqlr = cmd.ExecuteReader();

            while (sqlr.Read())
            {
                string value;

                if (sqlr.GetFieldAffinity(1) == TypeAffinity.Text)
                {
                    value = sqlr.GetString(1);
                }
                else if (sqlr.GetFieldAffinity(1) == TypeAffinity.Int64)
                {
                    value = sqlr.GetInt32(1).ToString();
                }
                else if (sqlr.GetFieldAffinity(1) == TypeAffinity.Double)
                {
                    value = sqlr.GetDouble(1).ToString(CultureInfo.InvariantCulture);
                }
                else
                {
                    throw new Exception($"unknown type: {sqlr.GetFieldAffinity(1)}");
                }

                int mediaId = sqlr.GetInt32(0);
                string tagIdentifier = sqlr.GetString(2);

                if (items.TryGetValue(mediaId, out PseMediaItem? item))
                    item.PseMetadataValues.Add(tagIdentifier, value);
                else
                    m_log.Add($"could not find media {mediaId} referenced in metadata {tagIdentifier}");
            }
        }
        finally
        {
            cmd.Close();
            sqlr?.Close();
        }

        if (m_log.Count > 0)
            MessageBox.Show($"warnings: {string.Join(",", m_log)}");
    }

    private static readonly string s_queryMediaImportDates = @"
        SELECT $$media_table$$.id, $$tag_table$$.name 
            FROM $$#media_table$$
            INNER JOIN $$#tag_to_media_table$$ on $$tag_to_media_table$$.media_id = $$media_table$$.id
            INNER JOIN $$#tag_table$$ on $$tag_table$$.id = $$tag_to_media_table$$.tag_id
            WHERE $$tag_table$$.type_name = 'import' ";

    
    void ReadMediaImportDates(Dictionary<int, PseMediaItem> items)
    {
        List<PseMediaImportItem> imports;

        try
        {
            imports = _Connection.DoGenericQueryDelegateRead(
                Guid.NewGuid(),
                s_queryMediaImportDates,
                s_aliases,
                (ISqlReader reader, Guid crid, ref List<PseMediaImportItem> building) =>
                {
                    building.Add(
                        new PseMediaImportItem()
                        {
                            ID = reader.GetInt32(0),
                            Name = reader.GetString(1)
                        });
                });
        }
        catch (TcSqlExceptionNoResults)
        {
            imports = new List<PseMediaImportItem>();
        }

        // now add the dates to the media
        foreach (PseMediaImportItem import in imports)
        {
            if (import.Name == null)
                throw new CatExceptionServiceDataFailure("name not set in import item");

            if (items.TryGetValue(import.ID, out PseMediaItem? item))
            {
                DateTime date = DateTime.Parse(import.Name[7..]);

                item.ImportDate = date.ToLocalTime();
            }
        }
    }

    private static readonly string s_queryMediaTags = @"
        SELECT TMT.media_id, TT.id
            FROM tag_to_media_table TMT
        INNER JOIN tag_table TT on TT.id=TMT.tag_id";

    void ReadMediaMetatags(MetatagMigrate metatagMigrate, Dictionary<int, PseMediaItem> items)
    {
        SqlSelect selectTags = new SqlSelect();

        selectTags.AddBase(s_queryMediaTags);
        selectTags.AddAliases(s_aliases);

        string sQuery = selectTags.ToString();

        ISqlReader? sqlr = null;
        ISqlCommand cmd = _Connection.CreateCommand();
        cmd.CommandText = sQuery;

        try
        {
            sqlr = cmd.ExecuteReader();

            while (sqlr.Read())
            {
                int mediaId = sqlr.GetInt32(0);
                int tagId = sqlr.GetInt32(1);

                PseMetatag? metatag = metatagMigrate.TryGetMetatagFromID(tagId);

                // there are many metatags we don't use (history, import, etc)
                if (metatag == null)
                    continue;
                // TODO NEED TO FIGURE OUT WHY/HOW THE pseOriginalFileDate (which is a builtin)
                // is not getting set. It has a setter in the tag mapping -- are these hooked up?
                if (items.TryGetValue(mediaId, out PseMediaItem? item))
                    item.PseMetatags.Add(metatag);
                else
                    m_log.Add($"could not find media {mediaId} referenced in metatag {tagId}");
            }

            if (m_log.Count > 0)
                MessageBox.Show($"warnings: {string.Join(",", m_log)}");
        }
        finally
        {
            cmd.Close();
            sqlr?.Close();
        }

        if (m_log.Count > 0)
            MessageBox.Show($"warnings: {string.Join(",", m_log)}");
    }

    private List<string> m_log = new();

    private static readonly string s_queryMediaDictionary = @"
        SELECT 
            MT.id, MT.full_filepath, MT.filepath_search_index, MT.filename_search_index, MT.mime_type, 
            MT.volume_id, VT.serial 
        FROM media_table as MT 
        INNER JOIN volume_table as VT on MT.volume_id = VT.id";

    private Dictionary<int, PseMediaItem> ReadMediaDictionary()
    {
        try
        {
            return
                _Connection.DoGenericQueryDelegateRead(
                    Guid.NewGuid(),
                    s_queryMediaDictionary,
                    s_aliases,
                    (ISqlReader reader, Guid crids, ref Dictionary<int, PseMediaItem> building) =>
                    {
                        int pseId = reader.GetInt32(0);

                        building.Add(
                            pseId,
                            PseMediaItemBuilder
                               .Create()
                               .SetID(pseId)
                               .SetFilename(reader.GetString(3))
                               .SetFilePath(reader.GetString(2))
                               .SetFullPath(reader.GetString(1))
                               .SetMimeType(reader.GetString(4))
                               .SetVolumeId(reader.GetInt32(5).ToString())
                               .SetVolumeName(reader.GetString(6))
                               .Build());
                    });
        }
        catch (TcSqlExceptionNoResults)
        {
            return new Dictionary<int, PseMediaItem>();
        }
    }

    public IEnumerable<PseMediaItem> ReadMediaItems(MetatagMigrate metatagMigrate)
    {
        Dictionary<int, PseMediaItem> map = ReadMediaDictionary();
        ReadMediaTagValues(map);
        ReadMediaMetatags(metatagMigrate, map);
        ReadMediaImportDates(map);
        return map.Values;
    }

    public List<PseStackItem> ReadVersionStacks()
    {
        try
        {
            return
                _Connection.DoGenericQueryDelegateRead(
                    Guid.NewGuid(),
                    s_queryVersionStacks,
                    s_aliases,
                    (ISqlReader reader, Guid crids, ref List<PseStackItem> building) =>
                    {
                        building.Add(
                            new PseStackItem(reader.GetInt32(0), reader.GetInt32(1), reader.GetInt32(2)));

                    });
        }
        catch (TcSqlExceptionNoResults)
        {
            return new List<PseStackItem>();
        }
    }

    public List<PseStackItem> ReadMediaStacks()
    {
        try
        {
            return
                _Connection.DoGenericQueryDelegateRead(
                    Guid.NewGuid(),
                    s_queryMediaStacks,
                    s_aliases,
                    (ISqlReader reader, Guid crids, ref List<PseStackItem> building) =>
                    {
                        building.Add(
                            new PseStackItem(reader.GetInt32(0), reader.GetInt32(1), reader.GetInt32(2)));
                    });
        }
        catch (TcSqlExceptionNoResults)
        {
            return new List<PseStackItem>();
        }
    }

}
