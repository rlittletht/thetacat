using System.Text.RegularExpressions;
using TCore;
using Thetacat.TCore.TcSqlLite;
using CustomizeCommandDelegate = Thetacat.TCore.TcSqlLite.CustomizeCommandDelegate;

namespace Tests.Model.Sql;

public class SqlSimQueryDataItem: ISqlReader
{
    public delegate bool FMatchDelegate(string query);
    private readonly object?[][] m_data;

    private readonly FMatchDelegate m_matchDelegate;

    public bool FMatch(string query) => m_matchDelegate(query);
    public bool RemoveAfterMatch { get; }
    public bool ShouldSkip { get; set; } = false;

    public void ResetRecordPointer() => record = -1;

    public SqlSimQueryDataItem(FMatchDelegate matchDelegate, object?[][] data, bool removeAfterMatch = true)
    {
        m_matchDelegate = matchDelegate;
        m_data = data;
        RemoveAfterMatch = removeAfterMatch;
    }

    private int record = -1;

    public void ExecuteQuery(SqlCommandTextInit cmdText, string sResourceConnString, CustomizeCommandDelegate? customizeDelegate = null) => throw new NotImplementedException();
    public void ExecuteQuery(string sQuery, string? sResourceConnString, CustomizeCommandDelegate? customizeDelegate = null, Dictionary<string, string>? aliases = null) => throw new NotImplementedException();

    public short GetInt16(int index) => (Int16)(m_data[record][index] ?? throw new NullReferenceException());
    public int GetInt32(int index) => (Int32)(m_data[record][index] ?? throw new NullReferenceException());
    public string GetString(int index) => (string)(m_data[record][index] ?? throw new NullReferenceException());
    public Guid GetGuid(int index) => Guid.Parse((string)(m_data[record][index] ?? throw new NullReferenceException()));
    public double GetDouble(int index) => (double)(m_data[record][index] ?? throw new NullReferenceException());
    public long GetInt64(int index) => (Int64)(m_data[record][index] ?? throw new NullReferenceException());
    public DateTime GetDateTime(int index) => DateTime.Parse((string)(m_data[record][index] ?? throw new NullReferenceException()));

    public short? GetNullableInt16(int index) => (Int16?)(m_data[record][index]);
    public int? GetNullableInt32(int index) => (Int32?)(m_data[record][index]);
    public string? GetNullableString(int index) => (string?)(m_data[record][index]);
    public Guid? GetNullableGuid(int index) => m_data[record][index] == null ? null : Guid.Parse((string)m_data[record][index]!);
    public double? GetNullableDouble(int index) => (double?)(m_data[record][index]);
    public long? GetNullableInt64(int index) => (Int64?)(m_data[record][index]);
    public DateTime? GetNullableDateTime(int index) => m_data[record][index] == null ? null : DateTime.Parse((string)m_data[record][index]!);

    public bool NextResult() => throw new NotImplementedException();

    public bool Read()
    {
        record++;
        return (record < m_data.Length);
    }

    public void Close()
    {
    }
}
