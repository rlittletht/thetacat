using System;
using System.Collections.Generic;
using System.Data.SQLite;
using TCore;

namespace Thetacat.TCore.TcSqlLite;

public class SQLite
{
    public delegate void CustomizeCommandDelegate(SQLiteCommand command);

    private readonly SQLiteConnection? m_sqlc;
    private readonly SQLiteTransaction? m_sqlt;

    public SQLite()
    {
        m_sqlc = null;
        m_sqlt = null;
    }

    public SQLite(SQLiteConnection sqlc, SQLiteTransaction? sqlt)
    {
        m_sqlc = sqlc;
        m_sqlt = sqlt;
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

    public SQLiteTransaction? Transaction => m_sqlt;

    public static SQLite OpenConnection(string sResourceConnString)
    {
        SQLiteConnection sqlc = new SQLiteConnection(sResourceConnString);

        sqlc.Open();

        return new SQLite(sqlc, null);
    }

    public SQLiteCommand CreateCommand()
    {
        return Connection.CreateCommand();
    }

    public void ExecuteNonQuery(
        string s,
        SQLite.CustomizeCommandDelegate? customizeParams = null,
        Dictionary<string, string>? aliases = null)
    {
        SQLiteCommand sqlcmd = CreateCommand();

        if (aliases != null)
            s = SqlWhere.ExpandAliases(s, aliases);

        sqlcmd.CommandText = s;
        if (customizeParams != null)
            customizeParams(sqlcmd);

        sqlcmd.Transaction = Transaction;
        sqlcmd.ExecuteNonQuery();
    }

    public void Close()
    {
        m_sqlt?.Dispose();
        m_sqlc?.Close();
        m_sqlc?.Dispose();
    }
}
