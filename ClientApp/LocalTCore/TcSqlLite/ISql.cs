using System.Collections.Generic;
using System;
using TCore;

namespace Thetacat.TCore.TcSqlLite;

public interface ISql
{

    public bool InTransaction { get; }

    public SQLiteTransaction? Transaction { get; }

    public ISqlCommand CreateCommand();
    public void ExecuteNonQuery(
        string s,
        CustomizeCommandDelegate? customizeParams = null,
        Dictionary<string, string>? aliases = null);

    public void ExecuteNonQuery(
        SqlCommandTextInit cmdText,
        CustomizeCommandDelegate? customizeParams = null);

    public T DoGenericQueryDelegateRead<T>(
        Guid crids,
        string query,
        Dictionary<string, string>? aliases,
        ISqlReader.DelegateReader<T> delegateReader,
        CustomizeCommandDelegate? customizeDelegate = null) where T : new();

    public int NExecuteScalar(string sQuery, Dictionary<string, string>? aliases = null);
    public int NExecuteScalar(SqlCommandTextInit cmdText);
    public void BeginExclusiveTransaction();
    public void BeginTransaction();
    public void Rollback();
    public void Commit();
    public void Close();
}
