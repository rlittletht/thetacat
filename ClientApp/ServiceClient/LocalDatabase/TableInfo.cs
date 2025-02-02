using System;
using System.Collections.Generic;
using System.Security.Permissions;
using TCore.SqlCore;

namespace Thetacat.ServiceClient.LocalDatabase;

public class TableInfo
{
    public bool IsTableDefined { get; set; } = false;

    private List<TableInfoItem> m_columns;

    private static readonly string s_queryTableInfo = @"
        pragma table_info";

    public TableInfo()
    {
        m_columns = new List<TableInfoItem>();
    }

    public TableInfo(List<TableInfoItem> columns)
    {
        IsTableDefined = true;
        m_columns = columns;
    }

    public static TableInfo CreateTableInfo(ISql sql, string tableName)
    {
        try
        {
            List<TableInfoItem> columns = sql.ExecuteDelegatedQuery(
                Guid.NewGuid(),
                $"{s_queryTableInfo}({tableName})",
                (ISqlReader reader, Guid crid, ref List<TableInfoItem> building) =>
                {
                    building.Add(
                        new TableInfoItem(
                            reader.GetInt32(0),
                            reader.GetString(1),
                            reader.GetString(2),
                            reader.GetInt32(3),
                            reader.GetNullableString(4),
                            reader.GetInt32(5)));
                });

            return new TableInfo(columns);
        }
        catch (SqlExceptionNoResults)
        {
            return new TableInfo();
        }
    }

    public bool IsColumnDefined(string columnName)
    {
        foreach (TableInfoItem column in m_columns)
        {
            if (string.Compare(column.Name, columnName, StringComparison.OrdinalIgnoreCase) == 0)
                return true;
        }

        return false;
    }
}
