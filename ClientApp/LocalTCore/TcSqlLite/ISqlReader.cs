using System;
using System.Collections.Generic;
using System.Data.SQLite;
using TCore;
using CustomizeCommandDelegate = Thetacat.TCore.TcSqlLite.CustomizeCommandDelegate;

namespace Thetacat.TCore.TcSqlLite;

public interface ISqlReader
{
    public delegate void DelegateMultiSetReader<T>(ISqlReader sqlr, Guid crids, int recordSet, ref T t);
    public delegate void DelegateReader<T>(ISqlReader sqlr, Guid crids, ref T t);

    public void ExecuteQuery(
        SqlCommandTextInit cmdText,
        string sResourceConnString,
        CustomizeCommandDelegate? customizeDelegate = null);

    public void ExecuteQuery(
        string sQuery,
        string? sResourceConnString,
        CustomizeCommandDelegate? customizeDelegate = null,
        Dictionary<string, string>? aliases = null);

    public Int16 GetInt16(int index);
    public Int32 GetInt32(int index);
    public string GetString(int index);
    public Guid GetGuid(int index);
    public double GetDouble(int index);
    public Int64 GetInt64(int index);
    public DateTime GetDateTime(int index);

    public Int16? GetNullableInt16(int index);
    public Int32? GetNullableInt32(int index);
    public string? GetNullableString(int index);
    public Guid? GetNullableGuid(int index);
    public double? GetNullableDouble(int index);
    public Int64? GetNullableInt64(int index);
    public DateTime? GetNullableDateTime(int index);

    public TypeAffinity GetFieldAffinity(int index);
    public bool NextResult();
    public bool Read();
    public void Close();
}
