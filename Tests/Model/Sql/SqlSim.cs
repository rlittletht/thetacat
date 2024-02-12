using TCore;
using TCore.SqlCore;
using CustomizeCommandDelegate = TCore.SqlCore.CustomizeCommandDelegate;

namespace Tests.Model.Sql;

public class SqlSim: ISql
{
    public bool InTransaction { get; }
    public ISqlTransaction? Transaction { get; }
    public ISqlCommand CreateCommand() => throw new NotImplementedException();

    private SqlSimQueryDataItem[]? m_data;
    private SqlSimNonQueryDataItem[]? m_nonQueryValidation;

    public void SetQuerySources(SqlSimQueryDataItem[] data)
    {
        m_data = data;
    }

    public void SetNonQueryValidation(SqlSimNonQueryDataItem[] validation)
    {
        m_nonQueryValidation = validation;
    }

    public void ExecuteNonQuery(string query, CustomizeCommandDelegate? customizeParams = null, TableAliases? aliases = null)
    {
        if (m_nonQueryValidation == null)
            throw new InvalidOperationException($"no data for nonquery sim: {query}");

        query = aliases?.ExpandAliases(query) ?? query;

        foreach (SqlSimNonQueryDataItem item in m_nonQueryValidation)
        {
            if (item.FMatch(query))
            {
                item.Validate(query);
                if (customizeParams != null && item.CommandExpected != null)
                    customizeParams(item.CommandExpected);
                return;
            }
        }

        throw new InvalidOperationException($"no match for query {query}");
    }

    public void ExecuteNonQuery(SqlCommandTextInit cmdText, CustomizeCommandDelegate? customizeParams = null)
    {
        ExecuteNonQuery(cmdText.CommandText, customizeParams, cmdText.Aliases);
    }

    public T DoGenericMultiSetQueryDelegateRead<T>(Guid crids, string sQuery, ISqlReader.DelegateMultiSetReader<T> delegateReader, CustomizeCommandDelegate? customizeDelegate = null) where T : new() => throw new NotImplementedException();

    public string SExecuteScalar(SqlCommandTextInit cmdText) => throw new NotImplementedException();

    public T DoGenericQueryDelegateRead<T>(
        Guid crids, string query, ISqlReader.DelegateReader<T> delegateReader,
        TableAliases? aliases = null,
        CustomizeCommandDelegate? customizeDelegate = null) where T : new()
    {
        if (m_data == null)
            throw new InvalidOperationException("no data for sim");

        query = aliases?.ExpandAliases(query) ?? query;

        if (m_data == null)
            throw new InvalidOperationException("no data for sim");

        // find the matching source data
        foreach (SqlSimQueryDataItem dataItem in m_data)
        {
            if (!dataItem.ShouldSkip && dataItem.FMatch(query))
            {
                dataItem.ResetRecordPointer();

                T t = new T();

                while (dataItem.Read())
                {
                    delegateReader(dataItem, crids, ref t);
                }

                if (dataItem.RemoveAfterMatch)
                    dataItem.ShouldSkip = true;
                return t;
            }
        }

        throw new InvalidOperationException($"no match for query {query}");
    }

    public int NExecuteScalar(string query, TableAliases? aliases = null)
    {
        query = aliases?.ExpandAliases(query) ?? query;

        if (m_data == null)
            throw new InvalidOperationException("no data for sim");

        // find the matching source data
        foreach (SqlSimQueryDataItem dataItem in m_data)
        {
            if (!dataItem.ShouldSkip && dataItem.FMatch(query))
            {
                dataItem.ResetRecordPointer();
                if (!dataItem.Read())
                    throw new InvalidOperationException("matched the Scalar query, but it didn't have a record");

                if (dataItem.RemoveAfterMatch)
                    dataItem.ShouldSkip = true;

                return (int)dataItem.GetInt32(0);
            }
        }

        throw new InvalidOperationException("no match for query");
    }

    public int NExecuteScalar(SqlCommandTextInit cmdText)
    {
        return NExecuteScalar(cmdText.CommandText, cmdText.Aliases);
    }

    public DateTime DttmExecuteScalar(SqlCommandTextInit cmdText) => throw new NotImplementedException();

    public void BeginExclusiveTransaction()
    {
    }

    public void BeginTransaction()
    {
    }

    public void Rollback()
    {
//        throw new NotImplementedException();
    }

    public void Commit()
    {
//        throw new NotImplementedException();
    }

    public void Close()
    {
//        throw new NotImplementedException();
    }
}
