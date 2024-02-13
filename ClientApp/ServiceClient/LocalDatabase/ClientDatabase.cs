using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using TCore.SqlCore;
using TCore;
using TCore.SqlClient;
using TCore.SQLiteClient;
using Thetacat.Model.Client;
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
    private static TableAliases s_aliases =
        new(
            new()
            {
                { "tcat_md5cache", "MDC" },
                { "tcat_derivatives", "DER" },
            });

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

        ISql connection = OpenDatabase();

        connection.ExecuteNonQuery(s_createMd5Cache);
        connection.ExecuteNonQuery(s_createDerivatives);

        connection.Close();

        return OpenDatabase();
    }

    public void Close()
    {
        m_connection?.Close();
    }

    private readonly string s_queryDerivatives = @"
        SELECT $$tcat_derivatives$$.media, $$tcat_derivatives$$.mimeType, $$tcat_derivatives$$.scaleFactor, $$tcat_derivatives$$.path
        FROM $$#tcat_derivatives$$";

    public List<DerivativeDbItem> ReadDerivatives()
    {
        try
        {
            return _Connection.ExecuteDelegatedQuery(
                Guid.NewGuid(),
                s_queryDerivatives,
                (ISqlReader reader, Guid crid, ref List<DerivativeDbItem> building) =>
                {
                    building.Add(
                        new DerivativeDbItem(
                            reader.GetGuid(0),
                            reader.GetString(1),
                            reader.GetDouble(2),
                            reader.GetString(3)));
                },
                s_aliases
                );
        }
        catch (SqlExceptionNoResults)
        {
            return new List<DerivativeDbItem>();
        }
    }


    string BuildDerivativeInsertCommand(DerivativeItem item)
    {
        return
            $"INSERT INTO tcat_derivatives (media, mimeType, scaleFactor, path) VALUES ({SqlText.SqlifyQuoted(item.MediaId.ToString())}, {SqlText.SqlifyQuoted(item.MimeType)}, {item.ScaleFactor}, {SqlText.SqlifyQuoted(item.Path)}) ";
    }

    string BuildDerivativeDeleteCommand(DerivativeItem item)
    {
        return $"DELETE FROM tcat_derivatives WHERE media={SqlText.SqlifyQuoted(item.MediaId.ToString())} AND mimeType={SqlText.SqlifyQuoted(item.MimeType)} AND scaleFactor={item.ScaleFactor}";
    }

    List<string> BuildDerivativeInsertCommands(IEnumerable<DerivativeItem> items)
    {
        List<string> commands = new List<string>();
        foreach (DerivativeItem item in items)
        {
            commands.Add(BuildDerivativeInsertCommand(item));
        }

        return commands;
    }

    List<string> BuildDerivativeDeleteCommands(IEnumerable<DerivativeItem> items)
    {
        List<string> commands = new List<string>();
        foreach (DerivativeItem item in items)
        {
            commands.Add(BuildDerivativeDeleteCommand(item));
        }

        return commands;
    }

    public void ExecuteDerivativeUpdates(IEnumerable<DerivativeItem> deletes, IEnumerable<DerivativeItem> inserts)
    {
        List<string> insertCommands = BuildDerivativeInsertCommands(inserts);
        List<string> deleteCommands = BuildDerivativeDeleteCommands(deletes);

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

    private readonly string s_queryAllMd5Cache = @"
        SELECT $$tcat_md5cache$$.path, $$tcat_md5cache$$.lastModified, 
            $$tcat_md5cache$$.size, $$tcat_md5cache$$.md5
        FROM $$#tcat_md5cache$$";

    public List<Md5CacheDbItem> ReadFullMd5Cache()
    {
        try
        {
            return _Connection.ExecuteDelegatedQuery(
                Guid.NewGuid(),
                s_queryAllMd5Cache,
                (ISqlReader reader, Guid crid, ref List<Md5CacheDbItem> building) =>
                {
                    building.Add(
                        new Md5CacheDbItem(
                            reader.GetString(0),
                            reader.GetString(3),
                            reader.GetDateTime(1),
                            reader.GetInt64(2)));
                },
                s_aliases);
        }
        catch (SqlExceptionNoResults)
        {
            return new List<Md5CacheDbItem>();
        }
    }

    string BuildMd5InsertCommand(Md5CacheItem item)
    {
        return $"INSERT INTO tcat_md5cache (path, md5, lastModified, size) VALUES ({SqlText.SqlifyQuoted(item.Path)}, {SqlText.SqlifyQuoted(item.MD5)}, '{item.LastModified.ToUniversalTime():u}', {item.Size}) ";
    }

    string BuildMd5DeleteCommand(Md5CacheItem item)
    {
        return $"DELETE FROM tcat_md5cache WHERE path={SqlText.SqlifyQuoted(item.Path)}";
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
