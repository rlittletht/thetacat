using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using Thetacat.Migration.Elements.Media;

namespace Thetacat.Migration.Elements.Metadata.UI;

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

useful queries:

    Show all the string-type metadata values for all of the media in the library. One row per metadata / media pair.

    SELECT MMT.media_id, MT.full_filepath, MST.value, MDT.identifier from media_to_metadata_table AS MMT
    INNER JOIN media_table MT on MT.id = MMT.media_id
    INNER JOIN metadata_string_table MST on MST.id = MMT.metadata_id
    INNER JOIN metadata_description_table MDT on MDT.id = MST.description_id    

 */
public class ElementsDb
{
    private SQLiteConnection? m_connection;

    public static ElementsDb Create(string databaseFile)
    {
        SQLiteConnection connection = new SQLiteConnection($"Data Source={databaseFile}");

        connection.Open();

        ElementsDb db = new()
                        {
                            m_connection = connection
                        };

        return db;
    }

    public void Close()
    {
        m_connection?.Close();
        m_connection?.Dispose();
    }

    static readonly string s_queryMetatagDefinitions = @"
        SELECT OTR.id, OTR.name, OTR.parent_id, OTR.type_name, INR.name AS ParentName
          FROM tag_table as OTR 
            INNER JOIN tag_table as INR 
              ON INR.id = OTR.parent_id 
         WHERE OTR.type_name NOT LIKE 'history%' AND OTR.type_name not like 'import%' AND OTR.type_name <> 'version_stack'";

    private static readonly string s_queryMetadataDefinitions = @"
        SELECT DESC.id, DESC.identifier, DESC.data_type
          FROM metadata_description_table as DESC";

    private string s_queryTagNotes = @"
        SELECT TT.ID, MS.value
          FROM tag_table as TT
            INNER JOIN tag_to_metadata_table TMD ON TMD.tag_id=TT.ID
            INNER JOIN metadata_string_table MS ON MS.id=TMD.metadata_id
            INNER JOIN metadata_description_table DT ON DT.id=MS.description_id
          WHERE DT.identifier = 'pse:TagNotes' AND value <> ''";

    public PseMetadataSchema ReadMetadataSchema()
    {
        using SQLiteCommand cmd = new()
                                  {
                                      CommandType = CommandType.Text,
                                      Connection = m_connection,
                                      Transaction = null,
                                  };

        cmd.CommandText = s_queryMetadataDefinitions;

        using SQLiteDataReader reader = cmd.ExecuteReader();

        List<PseMetadata> tags = new();

        while (reader.Read())
        {
            tags.Add(
                PseMetadataBuilder
                   .Create()
                   .SetPseId(reader.GetInt32(0))
                   .SetPseIdentifier(reader.GetString(1))
                   .SetPseDatatype(reader.GetString(2))
                   .Build());
        }

        reader.Close();
        return new PseMetadataSchema(tags);
    }

    Dictionary<int, PseMetatag> ReadMetatagDictionary()
    {
        using SQLiteCommand cmd =
            new()
            {
                CommandType = CommandType.Text,
                Connection = m_connection,
                Transaction = null,
            };

        cmd.CommandText = s_queryMetatagDefinitions;

        using SQLiteDataReader reader = cmd.ExecuteReader();

        Dictionary<int, PseMetatag> tags = new();

        while (reader.Read())
        {
            int pseId = reader.GetInt32(0);

            tags.Add(
                pseId,
                PseMetatagBuilder
                   .Create()
                   .SetID(pseId)
                   .SetName(reader.GetString(1))
                   .SetParentID(reader.GetInt32(2))
                   .SetElementsTypeName(reader.GetString(3))
                   .SetParentName(reader.GetString(4))
                   .Build());
        }

        reader.Close();
        return tags;
    }

    void ReadDescriptionsForMetatags(Dictionary<int, PseMetatag> tags)
    {
        using SQLiteCommand cmd =
            new()
            {
                CommandType = CommandType.Text,
                Connection = m_connection,
                Transaction = null,
            };

        cmd.CommandText = s_queryTagNotes;

        using SQLiteDataReader reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            int pseId = reader.GetInt32(0);
            string description = reader.GetString(1);

            if (tags.TryGetValue(pseId, out PseMetatag? tag))
                tag.Description = description;
        }

        reader.Close();
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

    private static string[] s_ignoreMetadata =
    {
        "pse:FileSize",
        "pse:FileSizeOriginal",
        "pse:TagIconMediaId",
        "pse:FileNameOriginal",
        "pse:TagNotes",
        "pre:ca:xmpPath",
        "pse:FaceDectectorBreezePath",
        "pse:FaceData",
        "pse:ImportSourceId",
        "pse:PrinterName",
        "pse:TagIconMediaCropRect",
        "pse:albumStyleXmlPath",
        "pse:guid",
        "pse:FileDate",
        "pse:FileDateOriginal",
        "pse:TagDate",
        "xmp:CreateDate"
    };

//    private static string s_queryReadStringMediaTagValues = @"
//        SELECT 
//";

    public List<MediaTagValue> ReadMediaTagValues()
    {
        using SQLiteCommand cmd = new()
                                  {
                                      CommandType = CommandType.Text,
                                      Connection = m_connection,
                                      Transaction = null,
                                  };


        using SQLiteDataReader reader = cmd.ExecuteReader();
        List<MediaTagValue> tags = new();
        while (reader.Read())
        {
        }

        return tags;
    }

    public List<MediaItem> ReadMediaItems()
    {
        using SQLiteCommand cmd = new()
                                  {
                                      CommandType = CommandType.Text,
                                      Connection = m_connection,
                                      Transaction = null,
                                  };

        cmd.CommandText =
            "select MT.id, MT.full_filepath, MT.filepath_search_index, MT.filename_search_index, MT.mime_type, MT.volume_id, VT.serial FROM media_table as MT INNER JOIN volume_table as VT on MT.volume_id = VT.id";

        using SQLiteDataReader reader = cmd.ExecuteReader();

        List<MediaItem> items = new();
        while (reader.Read())
        {
            items.Add(
                MediaItemBuilder
                   .Create()
                   .SetID(reader.GetInt32(0))
                   .SetFilename(reader.GetString(3))
                   .SetFilePath(reader.GetString(2))
                   .SetFullPath(reader.GetString(1))
                   .SetMimeType(reader.GetString(4))
                   .SetVolumeId(reader.GetInt32(5).ToString())
                   .SetVolumeName(reader.GetString(6))
                   .Build());
        }

        reader.Close();
        return items;
    }
}
