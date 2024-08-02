using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Windows;
using TCore.SqlCore;
using TCore;
using TCore.SqlClient;
using TCore.SQLiteClient;
using Thetacat.Model.Client;
using Thetacat.Types;
using Thetacat.Util;
using Thetacat.Model.Md5Caching;

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
            transformationsKey NVARCHAR(1024) NOT NULL,
            path VARCHAR(1024) NOT NULL,
            md5 NVARCHAR(32) NOT NULL,
            PRIMARY KEY (media, md5, mimeType, scaleFactor, transformationsKey))";

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
        SELECT $$tcat_derivatives$$.media, $$tcat_derivatives$$.mimeType, $$tcat_derivatives$$.scaleFactor, $$tcat_derivatives$$.transformationsKey, $$tcat_derivatives$$.path, $$tcat_derivatives$$.md5
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
                            reader.GetString(3),
                            reader.GetString(4),
                            reader.GetString(5)));
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
            $"INSERT INTO tcat_derivatives (media, mimeType, scaleFactor, transformationsKey, path, md5) VALUES ({SqlText.SqlifyQuoted(item.MediaId.ToString())}, {SqlText.SqlifyQuoted(item.MimeType)}, {item.ScaleFactor}, {SqlText.SqlifyQuoted(item.TransformationsKey)}, {SqlText.SqlifyQuoted(item.Path)}, {SqlText.SqlifyQuoted(item.MD5)}) ";
    }

    string BuildDerivativeDeleteCommand(DerivativeItem item)
    {
        return
            $"DELETE FROM tcat_derivatives WHERE media={SqlText.SqlifyQuoted(item.MediaId.ToString())} AND mimeType={SqlText.SqlifyQuoted(item.MimeType)} AND scaleFactor={item.ScaleFactor} AND MD5={SqlText.SqlifyQuoted(item.MD5)}";
    }

    List<string> BuildDerivativeInsertCommands(IEnumerable<DerivativeItem> items)
    {
        List<string> commands = new List<string>();
        foreach (DerivativeItem item in items)
        {
            if (!item.HasPath)
                continue;

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

    private static readonly string s_deleteMediaDerivatives = @"
        DELETE FROM tcat_derivatives WHERE media=@MediaId";

    public void DeleteMediaDerivatives(Guid id)
    {
        _Connection.ExecuteNonQuery(
            s_deleteMediaDerivatives,
            cmd => cmd.AddParameterWithValue("@MediaId", id.ToString()),
            s_aliases);
    }

    public void ExecuteDerivativeUpdates(IEnumerable<DerivativeItem> deletes, IEnumerable<DerivativeItem> inserts, IEnumerable<DerivativeItem> updates)
    {
        List<string> insertCommands = BuildDerivativeInsertCommands(inserts);
        List<string> deleteCommands = BuildDerivativeDeleteCommands(deletes);

        // we don't actually update expired items -- we delete them because a subsequent insert
        // will replace it. when we query again on the next session we will have the latest.

        // this is preferable because the derivatives on disk self-replace. (GUID-WxH-transform.jpg)
        List<string> updateCommands = BuildDerivativeDeleteCommands(updates);

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
                updateCommands,
                (line) => line,
                100,
                ";");
            _Connection.Commit();
            WorkgroupDb.ExecutePartedCommands(
                _Connection,
                "",
                insertCommands,
                (line) => line,
                100,
                ";");
            return;
        }
        catch
        {
            _Connection.Rollback();
            throw;
        }
    }

    private readonly string s_deleteMediaItemDerivatives = @"
        DELETE FROM tcat_derivatives WHERE media=@MediaId";

    public void DeleteMediaItemDerivatives(Guid itemId)
    {
        _Connection.ExecuteNonQuery(
            s_deleteMediaItemDerivatives,
            cmd => cmd.AddParameterWithValue("@MediaId", itemId.ToString()),
            s_aliases);
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
        return
            $"INSERT INTO tcat_md5cache (path, md5, lastModified, size) VALUES ({SqlText.SqlifyQuoted(item.Path)}, {SqlText.SqlifyQuoted(item.MD5)}, '{item.LastModified.ToUniversalTime():u}', {item.Size}) ";
    }

    string BuildMd5UpdateCommand(Md5CacheItem item)
    {
        return $"UPDATE tcat_md5cache SET md5={SqlText.SqlifyQuoted(item.MD5)}, lastModified='{item.LastModified.ToUniversalTime():u}', size={item.Size} WHERE path={SqlText.SqlifyQuoted(item.Path)}";
    }

    string BuildMd5DeleteCommand(Md5CacheItem item)
    {
        return $"DELETE FROM tcat_md5cache WHERE path={SqlText.SqlifyQuoted(item.Path)}";
    }

    void BuildMd5Commands(
        IEnumerable<Md5CacheItem> items,
        out List<string> insertCommands,
        out List<string> updateCommands,
        out List<string> deleteCommands)
    {
        insertCommands = new List<string>();
        updateCommands = new List<string>();
        deleteCommands = new List<string>();

        foreach (Md5CacheItem item in items)
        {
            if (item.ChangeState == ChangeState.Create)
                insertCommands.Add(BuildMd5InsertCommand(item));
            else if (item.ChangeState == ChangeState.Delete)
                deleteCommands.Add(BuildMd5DeleteCommand(item));
            else if (item.ChangeState == ChangeState.Update)
                updateCommands.Add(BuildMd5UpdateCommand(item));
        }
    }

    public void ExecuteMd5CacheUpdates(IEnumerable<Md5CacheItem> changes)
    {
        BuildMd5Commands(
            changes,
            out List<string> insertCommands,
            out List<string> updateCommands,
            out List<string> deleteCommands);

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
            WorkgroupDb.ExecutePartedCommands(
                _Connection,
                "",
                updateCommands,
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

    private readonly string s_copyFromOldDerivatives = @"
        INSERT INTO tcat_derivatives (media, mimeType, scaleFactor, transformationsKey, path, md5)
        SELECT media, mimeType, scaleFactor, transformationsKey, path, '' FROM tcat_derivatives_old";

    void CreateNewDerivativeTableAndPopulate()
    {
        // first, rename the existing table
        _Connection.ExecuteNonQuery(new SqlCommandTextInit("ALTER TABLE tcat_derivatives RENAME TO tcat_derivatives_old"));

        // now create the new table structure
        _Connection.ExecuteNonQuery(s_createDerivatives);

        // now populate from the old table
        _Connection.ExecuteNonQuery(new SqlCommandTextInit(s_copyFromOldDerivatives));

        // and drop the old table
        _Connection.ExecuteNonQuery(new SqlCommandTextInit("DROP TABLE tcat_derivatives_old"));
    }

    // make a class to get the results from the table info, then execute a query reader into it, then
    // check if it has the md5 column. if not, call the adjust derivatives and we should be good to go. to sleep.
    public void AdjustDatabaseIfNecessary()
    {
        // first figure out if we've got everything we need
        TableInfo info = TableInfo.CreateTableInfo(_Connection, "tcat_derivatives");

        if (!info.IsColumnDefined("md5"))
        {
            CreateNewDerivativeTableAndPopulate();
        }

        // and make sure they stick
        info = TableInfo.CreateTableInfo(_Connection, "tcat_derivatives");

        if (!info.IsColumnDefined("md5"))
            throw new CatExceptionInternalFailure("md5 column not defined even after we altered the table!");
    }
}
