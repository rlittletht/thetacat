using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using Thetacat.Migration.Elements.Media;

namespace Thetacat.Migration.Elements.Metadata.UI;

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

    /*----------------------------------------------------------------------------
        %%Function: ReadMetadataTags
        %%Qualified: Thetacat.Migration.Elements.ElementsDb.ReadMetadataTags

        This reads all the user defined/custom metatags that can be 'painted'
        on the media items
    ----------------------------------------------------------------------------*/
    public List<PseMetatag> ReadMetadataTags()
    {
        using SQLiteCommand cmd = new()
                                  {
                                      CommandType = CommandType.Text,
                                      Connection = m_connection,
                                      Transaction = null,
                                  };

        cmd.CommandText = s_queryMetatagDefinitions;

        using SQLiteDataReader reader = cmd.ExecuteReader();

        List<PseMetatag> tags = new();

        while (reader.Read())
        {
            tags.Add(
                PseMetatagBuilder
                   .Create()
                   .SetID(reader.GetInt32(0).ToString())
                   .SetName(reader.GetString(1))
                   .SetParentID(reader.GetInt32(2).ToString())
                   .SetElementsTypeName(reader.GetString(3))
                   .SetParentName(reader.GetString(4))
                   .Build());
        }

        reader.Close();
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
