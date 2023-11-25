using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thetacat.Migration.Elements;

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

    public List<ElementsMetaTag> ReadMetadataTags()
    {
        using SQLiteCommand cmd = new()
        {
            CommandType = CommandType.Text,
            Connection = m_connection,
            Transaction = null,
        };

        cmd.CommandText =
            "select OTR.id, OTR.name, OTR.parent_id, OTR.type_name, INR.name as ParentName FROM tag_table as OTR INNER JOIN tag_table as INR on INR.id = OTR.parent_id where OTR.parent_id <> 0 and OTR.type_name not like 'history%' and OTR.type_name not like 'import%'";

        using SQLiteDataReader reader = cmd.ExecuteReader();

        List<ElementsMetaTag> tags = new();

        while (reader.Read())
        {
            tags.Add(
                ElementsMetaTagBuilder
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

    public List<ElementsMediaItem> ReadMediaItems()
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

        List<ElementsMediaItem> items = new();
        while (reader.Read())
        {
            items.Add(
                ElementsMediaItemBuilder
                   .Create()
                   .SetID(reader.GetInt32(0).ToString())
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