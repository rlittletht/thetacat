﻿using System;
using System.Collections.Generic;
using System.Data.SQLite;
using TCore;

namespace Thetacat.TCore.TcSqlLite;

public class SQLiteReader
{
    private SQLite? m_sql;
    private bool m_fAttached = false;
    private SQLiteDataReader? m_sqlr = null;
    private Guid m_crids;

    public delegate void DelegateMultiSetReader<T>(SQLiteReader sqlr, Guid crids, int recordSet, ref T t);
    public delegate void DelegateReader<T>(SQLiteReader sqlr, Guid crids, ref T t);

    private SQLiteDataReader _Reader
    {
        get
        {
            if (m_sqlr == null)
                throw new Exception("no reader");
            return m_sqlr;
        }
    }
    public SQLiteReader()
    {
        m_fAttached = false;
        m_crids = Guid.Empty;
    }

    public SQLiteReader(Guid crids)
    {
        m_fAttached = false;
        m_crids = crids;
    }

    public SQLiteReader(SQLite sql)
    {
        Attach(sql);
        m_crids = Guid.Empty;
    }

    public SQLiteReader(SQLite sql, Guid crids)
    {
        Attach(sql);
        m_crids = crids;
    }

    /*----------------------------------------------------------------------------
        %%Function: Attach
        %%Qualified: TCore.SqlReader.Attach
    ----------------------------------------------------------------------------*/
    public void Attach(SQLite sql)
    {
        m_sql = sql;
        if (m_sql != null)
            m_fAttached = true;
    }

    public void ExecuteQuery(
        SqlCommandTextInit cmdText,
        string sResourceConnString,
        SQLite.CustomizeCommandDelegate? customizeDelegate = null)
    {
        ExecuteQuery(cmdText.CommandText, sResourceConnString, customizeDelegate, cmdText.Aliases);
    }

    public void ExecuteQuery(
        string sQuery,
        string? sResourceConnString,
        SQLite.CustomizeCommandDelegate? customizeDelegate = null,
        Dictionary<string, string>? aliases = null)
    {
        if (m_sql == null)
        {
            if (sResourceConnString == null)
                throw new ArgumentNullException(nameof(sResourceConnString));

            m_sql = SQLite.OpenConnection(sResourceConnString);
            m_fAttached = false;
        }

        if (m_sql == null)
            throw new TcSqlException("could not open sql connection");

        SQLiteCommand sqlcmd = m_sql.Connection.CreateCommand();
        sqlcmd.CommandText = sQuery;
        sqlcmd.Transaction = m_sql.Transaction;

        if (customizeDelegate != null)
            customizeDelegate(sqlcmd);

        if (m_sqlr != null)
        {
            m_sqlr.Close();
            m_sqlr.Dispose();
        }

        try
        {
            m_sqlr = sqlcmd.ExecuteReader();
        }
        catch (Exception exc)
        {
            throw new TcSqlException(m_crids, exc, "caught exception executing reader");
        }
    }

    /*----------------------------------------------------------------------------
        %%Function: DoGenericQueryDelegateRead
        %%Qualified: TCore.SqlReader.DoGenericQueryDelegateRead<T>
    ----------------------------------------------------------------------------*/
    public static T DoGenericQueryDelegateRead<T>(
        SQLite sql,
        Guid crids,
        string sQuery,
        DelegateReader<T> delegateReader,
        SQLite.CustomizeCommandDelegate? customizeDelegate = null) where T : new()
    {
        SQLiteReader? sqlr = null;

        if (delegateReader == null)
            throw new Exception("must provide delegate reader");

        try
        {
            string sCmd = sQuery;

            sqlr = new(sql);
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

    public static T DoGenericMultiSetQueryDelegateRead<T>(
        SQLite sql,
        Guid crids,
        string sQuery,
        DelegateMultiSetReader<T> delegateReader,
        SQLite.CustomizeCommandDelegate? customizeDelegate = null) where T : new()
    {
        SQLiteReader? sqlr = null;

        if (delegateReader == null)
            throw new Exception("must provide delegate reader");

        try
        {
            string sCmd = sQuery;

            sqlr = new(sql);
            sqlr.ExecuteQuery(sQuery, null, customizeDelegate);

            int recordSet = 0;

            T t = new();
            do
            {
                bool fOnce = false;

                while (sqlr.Read())
                {
                    delegateReader(sqlr, crids, recordSet, ref t);
                    fOnce = true;
                }

                if (!fOnce)
                    throw new TcSqlExceptionNoResults();

                recordSet++;
            } while (sqlr.NextResult());

            return t;
        }
        finally
        {
            sqlr?.Close();
        }
    }

    public Int16 GetInt16(int index) => _Reader.GetInt16(index);
    public Int32 GetInt32(int index) => _Reader.GetInt32(index);
    public string GetString(int index) => _Reader.GetString(index);
    public Guid GetGuid(int index) => _Reader.GetGuid(index);
    public double GetDouble(int index) => _Reader.GetDouble(index);
    public Int64 GetInt64(int index) => _Reader.GetInt64(index);


    public bool NextResult() => m_sqlr?.NextResult() ?? false;
    public bool Read() => m_sqlr?.Read() ?? false;

    public void Close()
    {
        m_sqlr?.Close();
        m_sqlr?.Dispose();

        if (!m_fAttached)
            m_sql?.Close();
    }
}