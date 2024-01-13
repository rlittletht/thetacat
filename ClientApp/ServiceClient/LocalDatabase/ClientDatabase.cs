using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using Emgu.CV.Util;
using TCore;
using Thetacat.Model.Client;
using Thetacat.TCore.TcSqlLite;
using Thetacat.Types;
using Thetacat.Util;

namespace Thetacat.ServiceClient.LocalDatabase;

/*----------------------------------------------------------------------------
    %%Class: ClientDatabase
    %%Qualified: Thetacat.ServiceClient.ClientDatabase

    Support for our local SQLite database
----------------------------------------------------------------------------*/
public class ClientDatabase
{
    private static Dictionary<string, string> s_aliases =
        new()
        {
            { "tcat_md5cache", "MDC" },
        };

    private readonly string m_database;
    private readonly ISql? m_connection;

    private ISql _Connection
    {
        get
        {
            if (m_connection == null)
                throw new CatExceptionInitializationFailure();

            return m_connection;
        }
    }

    public ClientDatabase(ISql sql)
    {
        m_database = new PathSegment("mock");
        m_connection = sql;
    }

    public ClientDatabase(string database)
    {
        m_database = database;

        if (!Path.Exists(m_database))
        {
            m_connection = CreateDatabase();
            return;
        }

        m_connection = OpenDatabase();
    }

    /*----------------------------------------------------------------------------
    %%Function: OpenDatabase
    %%Qualified: Thetacat.Model.WorkgroupDb.OpenDatabase
----------------------------------------------------------------------------*/
    private SQLite OpenDatabase()
    {
        SQLite connection = SQLite.OpenConnection($"Data Source={m_database}");

        return connection;
    }

    private readonly string s_createMd5Cache = @"
        CREATE TABLE tcat_md5cache
            (path VARCHAR(1024) NOT NULL,
            lastModified NVARCHAR(64) NOT NULL,
            size INTEGER NOT NULL,
            md5 NVARCHAR(32))";

    private readonly string s_createDerivatives = @"
        CREATE TABLE tcat_derivatives
            (media VARCHAR(36) NOT NULL,
            mimeType NVARCHAR(36) NOT NULL,
            scaleFactor REAL NOT NULL,
            path VARCHAR(1024) NOT NULL,
            PRIMARY KEY (media, mimeType, scaleFactor))";

    /*----------------------------------------------------------------------------
        %%Function: CreateDatabase
        %%Qualified: Thetacat.Model.WorkgroupDb.CreateDatabase
    ----------------------------------------------------------------------------*/
    private SQLite CreateDatabase()
    {
        SQLiteConnection.CreateFile(m_database);

        SQLite connection = OpenDatabase();

        connection.ExecuteNonQuery(s_createMd5Cache);
        connection.ExecuteNonQuery(s_createDerivatives);

        connection.Close();

        return OpenDatabase();
    }

    public void Close()
    {
        m_connection?.Close();
    }

    private readonly string s_queryAllMd5Cache = @"
        SELECT $$tcat_md5cache$$.path, $$tcat_md5cache$$.lastModified, 
            $$tcat_md5cache$$.size, $$tcat_md5cache$$.md5
        FROM $$#tcat_md5cache$$";

    public List<Md5CacheDbItem> ReadFullMd5Cache()
    {
        try
        {
            return _Connection.DoGenericQueryDelegateRead(
                Guid.NewGuid(),
                s_queryAllMd5Cache,
                s_aliases,
                (ISqlReader reader, Guid crid, ref List<Md5CacheDbItem> building) =>
                {
                    building.Add(
                        new Md5CacheDbItem(
                            reader.GetString(0),
                            reader.GetString(3),
                            reader.GetDateTime(1),
                            reader.GetInt64(2)));
                });
        }
        catch (TcSqlExceptionNoResults)
        {
            return new List<Md5CacheDbItem>();
        }
    }

    string BuildMd5InsertCommand(Md5CacheItem item)
    {
        return $"INSERT INTO tcat_md5cache (path, md5, lastModified, size) VALUES ('{Sql.Sqlify(item.Path)}', '{Sql.Sqlify(item.MD5)}', '{item.LastModified.ToUniversalTime().ToString("u")}', {item.Size}) ";
    }

    string BuildMd5DeleteCommand(Md5CacheItem item)
    {
        return $"DELETE ROM tcat_md5cache WHERE path='{Sql.Sqlify(item.Path)}'";
    }

    List<string> BuildMd5InsertCommands(IEnumerable<Md5CacheItem> items)
    {
        List<string> commands = new List<string>();
        foreach (Md5CacheItem item in items)
        {
            commands.Add(BuildMd5InsertCommand(item));
        }
        return commands ;
    }

    List<string> BuildMd5DeleteCommands(IEnumerable<Md5CacheItem> items)
    {
        List<string> commands = new List<string>();
        foreach (Md5CacheItem item in items)
        {
            commands.Add(BuildMd5DeleteCommand(item));
        }

        return commands;
    }

    public void ExecuteMd5CacheUpdates(IEnumerable<Md5CacheItem> deletes, IEnumerable<Md5CacheItem> inserts)
    {
        List<string> insertCommands = BuildMd5InsertCommands(inserts);
        List<string> deleteCommands = BuildMd5DeleteCommands(deletes);

        _Connection.BeginTransaction();
        try
        {
            WorkgroupDb.ExecutePartedCommands(
                _Connection,
                "",
                deleteCommands,
                (line) => line,
                100,
                ";");
            WorkgroupDb.ExecutePartedCommands(
                _Connection,
                "",
                insertCommands,
                (line) => line,
                100,
                ";");
            _Connection.Commit();
            return;
        }
        catch
        {
            _Connection.Rollback();
            throw;
        }
    }
}
