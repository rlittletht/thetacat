using System.Data.SQLite;
using TCore;
using Thetacat.TCore.TcSqlLite;
using CustomizeCommandDelegate = Thetacat.TCore.TcSqlLite.CustomizeCommandDelegate;

namespace Tests.Model.Sql;

public class SqlReaderSim: ISqlReader
{
    private SqlSimQueryDataItem m_dataSource;

    public SqlReaderSim(SqlSimQueryDataItem queryDataItem)
    {
        m_dataSource = queryDataItem;
    }

    public void ExecuteQuery(SqlCommandTextInit cmdText, string sResourceConnString, CustomizeCommandDelegate? customizeDelegate = null)
    {
        throw new NotImplementedException();
    }

    public void ExecuteQuery(string sQuery, string? sResourceConnString, CustomizeCommandDelegate? customizeDelegate = null, Dictionary<string, string>? aliases = null)
    {
        throw new NotImplementedException();
    }

    public short GetInt16(int index) => throw new NotImplementedException();

    public int GetInt32(int index) => throw new NotImplementedException();

    public string GetString(int index) => throw new NotImplementedException();

    public Guid GetGuid(int index) => throw new NotImplementedException();

    public double GetDouble(int index) => throw new NotImplementedException();

    public long GetInt64(int index) => throw new NotImplementedException();

    public DateTime GetDateTime(int index) => throw new NotImplementedException();

    public short? GetNullableInt16(int index) => throw new NotImplementedException();

    public int? GetNullableInt32(int index) => throw new NotImplementedException();

    public string? GetNullableString(int index) => throw new NotImplementedException();

    public Guid? GetNullableGuid(int index) => throw new NotImplementedException();

    public double? GetNullableDouble(int index) => throw new NotImplementedException();

    public long? GetNullableInt64(int index) => throw new NotImplementedException();

    public DateTime? GetNullableDateTime(int index) => throw new NotImplementedException();
    public TypeAffinity GetFieldAffinity(int index) => throw new NotImplementedException();

    public bool NextResult() => throw new NotImplementedException();

    public bool Read() => throw new NotImplementedException();

    public void Close()
    {
        throw new NotImplementedException();
    }
}
