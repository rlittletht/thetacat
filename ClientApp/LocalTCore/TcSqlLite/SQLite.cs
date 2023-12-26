using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using TCore;

namespace Thetacat.TCore.TcSqlLite;

public class SQLite : ISql
{
    private readonly SQLiteConnection? m_sqlc;
    public bool InTransaction  => m_transaction != null;

    public static string Iso8601DateFromPackedSqliteDate(string packedDate)
    {
        if (packedDate.Contains("-"))
            return packedDate;

        if (packedDate[8] != 'T' || packedDate.Length < 14)
            throw new ArgumentException($"{packedDate} is not in format YYYYMMDDTHHMMSS[.ssssssss]");

        return $"{packedDate[0..4]}-{packedDate[4..6]}-{packedDate[6..8]}T{packedDate[9..11]}:{packedDate[11..13]}:{packedDate[13..]}";
    }

    public SQLite()
    {
        m_sqlc = null;
        m_transaction = null;
    }

    public SQLite(SQLiteConnection sqlc, SQLiteTransaction? sqlt)
    {
        m_sqlc = sqlc;
        m_transaction = sqlt;
    }

    public SQLiteConnection Connection
    {
        get
        {
            if (m_sqlc == null)
                throw new Exception("no connection");

            return m_sqlc;
        }
    }

    // we can't just call SqlWhere.ExpandAliases since the mapping order is different
    // internally to Sql and externally in "SqlSelect.AddAliases"
    public static string ExpandAliasesProperly(string query, Dictionary<string, string>? aliases)
    {
        if (aliases == null)
            return query;

        SqlSelect selectTags = new SqlSelect();

        selectTags.AddBase(query);
        selectTags.AddAliases(aliases);

        return selectTags.ToString();
    }

    public SQLiteTransaction? Transaction => m_transaction;

    public static SQLite OpenConnection(string sResourceConnString)
    {
        SQLiteConnection sqlc = new SQLiteConnection(sResourceConnString);

        sqlc.Open();

        return new SQLite(sqlc, null);
    }

    public ISqlCommand CreateCommand()
    {
        return new TcSqlLite.SQLiteCommand(Connection.CreateCommand());
    }

    public SQLiteCommand CreateCommandInternal()
    {
        return new TcSqlLite.SQLiteCommand(Connection.CreateCommand());
    }

    public void ExecuteNonQuery(
        string s,
        CustomizeCommandDelegate? customizeParams = null,
        Dictionary<string, string>? aliases = null)
    {
        ISqlCommand sqlcmd = CreateCommand();

        try
        {
            if (aliases != null)
                s = ExpandAliasesProperly(s, aliases);

            sqlcmd.CommandText = s;
            if (customizeParams != null)
                customizeParams(sqlcmd);

            if (Transaction != null)
                sqlcmd.Transaction = Transaction;

            sqlcmd.ExecuteNonQuery();
        }
        finally
        {
            sqlcmd.Close();
        }
    }

    public void ExecuteNonQuery(
        SqlCommandTextInit cmdText,
        CustomizeCommandDelegate? customizeParams = null)
    {
        ExecuteNonQuery(cmdText.CommandText, customizeParams, cmdText.Aliases);
    }

    public int NExecuteScalar(string sQuery, Dictionary<string, string>? aliases = null)
    {
        ISqlCommand sqlcmd = CreateCommand();

        try
        {
            if (aliases != null)
                sQuery = ExpandAliasesProperly(sQuery, aliases);

            sqlcmd.CommandText = sQuery;
            if (Transaction != null)
                sqlcmd.Transaction = this.Transaction;

            Int64 n = (Int64)sqlcmd.ExecuteScalar();

            return (int)n;
        }
        finally
        {
            sqlcmd.Close();
        }
    }

    public int NExecuteScalar(SqlCommandTextInit cmdText)
    {
        return NExecuteScalar(cmdText.CommandText, cmdText.Aliases);
    }

    public T DoGenericQueryDelegateRead<T>(
        Guid crids,
        string query,
        Dictionary<string, string>? aliases,
        ISqlReader.DelegateReader<T> delegateReader,
        CustomizeCommandDelegate? customizeDelegate = null) where T : new()
    {
        SqlSelect selectTags = new SqlSelect();

        selectTags.AddBase(query);
        if (aliases != null)
            selectTags.AddAliases(aliases);

        string sQuery = selectTags.ToString();

        SQLiteReader? sqlr = null;

        if (delegateReader == null)
            throw new Exception("must provide delegate reader");

        try
        {
            string sCmd = sQuery;

            sqlr = new(this);
            sqlr.ExecuteQuery(sQuery, null, customizeDelegate);

            T t = new();
            bool fOnce = false;

            while (sqlr.Read())
            {
                delegateReader(sqlr, crids, ref t);
                fOnce = true;
            }

            if (!fOnce)
                throw new TcSqlExceptionNoResults();

            return t;
        }
        finally
        {
            sqlr?.Close();
        }
    }

    private SQLiteTransaction? m_transaction;

    public void BeginTransaction()
    {
        if (InTransaction)
            throw new TcSqlExceptionInTransaction();

        SQLiteReader.ExecuteWithDatabaseLockRetry(
            () => m_transaction = new SQLiteTransaction(Connection.BeginTransaction()),
            250,
            5000);
    }

    public void BeginExclusiveTransaction()
    {
        if (InTransaction)
            throw new TcSqlExceptionInTransaction();

        SQLiteReader.ExecuteWithDatabaseLockRetry(
#pragma warning disable CS0618
            () => m_transaction = new SQLiteTransaction(Connection.BeginTransaction(IsolationLevel.Serializable, false)),
#pragma warning restore CS0618
            250,
            5000);
    }

    public void Rollback()
    {
        if (!InTransaction)
            throw new TcSqlExceptionNotInTransaction();

        m_transaction!.Rollback();
        m_transaction.Dispose();
        m_transaction = null;
    }

    public void Commit()
    {
        if (!InTransaction)
            throw new TcSqlExceptionNotInTransaction();

        m_transaction!.Commit();
        m_transaction.Dispose();
        m_transaction = null;
    }


    public void Close()
    {
        if (InTransaction)
            Rollback();

        m_transaction?.Dispose();
        m_sqlc?.Close();
        m_sqlc?.Dispose();
    }
}
