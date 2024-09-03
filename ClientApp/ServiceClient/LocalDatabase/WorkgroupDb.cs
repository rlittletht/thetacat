using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Text;
using TCore;
using TCore.SqlCore;
using TCore.SqlClient;
using TCore.SQLiteClient;
using Thetacat.Model.Workgroups;
using Thetacat.Types;
using Thetacat.Util;

namespace Thetacat.ServiceClient.LocalDatabase;

public class WorkgroupDb
{
    private static TableAliases s_aliases =
        new(
            new()
            {
                { "tcat_workgroup_media", "TWM" },
                { "tcat_workgroup_clients", "TWC" }
            });

    private readonly PathSegment m_database;
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

    public WorkgroupDb(ISql sql)
    {
        m_database = new PathSegment("mock");
        m_connection = sql;
    }

    /*----------------------------------------------------------------------------
        %%Function: WorkgroupDb
        %%Qualified: Thetacat.Model.WorkgroupDb.WorkgroupDb
    ----------------------------------------------------------------------------*/
    public WorkgroupDb(PathSegment database)
    {
        m_database = database;

        if (!Path.Exists(m_database.Local))
        {
            m_connection = CreateDatabase();
            return;
        }

        m_connection = OpenDatabase();
    }

    private readonly string s_createWorkgroupMedia = @"
        CREATE TABLE tcat_workgroup_media
            (media VARCHAR(36) NOT NULL PRIMARY KEY, 
            path VARCHAR(1024) NOT NULL,
            md5 NVARCHAR(32) NOT NULL,
            cachedBy VARCHAR(36) NOT NULL, 
            cachedDate NVARCHAR(64), 
            vectorClock INTEGER NOT NULL)";

    private readonly string s_createWorkgroupFilters = @"
        CREATE TABLE tcat_workgroup_filters
            (id VARCHAR(36) NOT NULL PRIMARY KEY, 
            name VARCHAR(64) NOT NULL,
            description VARCHAR(256) NOT NULL,
            expression VARCHAR(1024) NOT NULL, 
            vectorClock INTEGER NOT NULL)";

    private readonly string s_createWorkgroupVClock = @"
        CREATE TABLE tcat_workgroup_vectorclock(
            clock VARCHAR(24) NOT NULL PRIMARY KEY, 
            value INTEGER NOT NULL)";

    private readonly string s_createWorkgroupClients = @"
        CREATE TABLE tcat_workgroup_clients(
            id VARCHAR(36) NOT NULL PRIMARY KEY, 
            name VARCHAR(128) NOT NULL, 
            vectorClock INTEGER NOT NULL)";

    private readonly string s_initializeVectorClock = @"
        INSERT INTO tcat_workgroup_vectorclock (clock, value) VALUES ('workgroup-clock', 0)";

    private readonly string s_initializeFilterVectorClock = @"
        INSERT INTO tcat_workgroup_vectorclock (clock, value) VALUES ('filter-clock', 0)";

    private readonly string s_queryWorkgroupClientDetailsByName = @"
        SELECT $$tcat_workgroup_clients$$.id, $$tcat_workgroup_clients$$.name, $$tcat_workgroup_clients$$.vectorClock
        FROM $$#tcat_workgroup_clients$$
        WHERE $$tcat_workgroup_clients$$.name = @Name";

    private readonly string s_queryWorkgroupMediaClock = @"
        SELECT $$tcat_workgroup_media$$.media,$$tcat_workgroup_media$$.path, $$tcat_workgroup_media$$.cachedBy, $$tcat_workgroup_media$$.cachedDate, $$tcat_workgroup_media$$.vectorClock, $$tcat_workgroup_media$$.md5
        FROM $$#tcat_workgroup_media$$
        INNER JOIN $$#tcat_workgroup_clients$$ ON $$tcat_workgroup_media$$.cachedBy = $$tcat_workgroup_clients$$.id";

    private readonly string s_queryWorkgroupFilters = @"
        SELECT $$tcat_workgroup_filters$$.id,$$tcat_workgroup_filters$$.name, $$tcat_workgroup_filters$$.description, $$tcat_workgroup_filters$$.expression, $$tcat_workgroup_filters$$.vectorClock
        FROM $$#tcat_workgroup_filters$$";

    private readonly string s_queryWorkgroupClock = @"
        SELECT value FROM tcat_workgroup_vectorclock WHERE clock = 'workgroup-clock'";

    private readonly string s_insertWorkgroupClient = @"
        INSERT INTO tcat_workgroup_clients (id, name, vectorClock) VALUES (@Id, @Name, @VectorClock)";

    private readonly string s_updateWorkgroupClock = @"
        UPDATE tcat_workgroup_vectorclock SET value = @VectorClock WHERE clock = 'workgroup-clock'";

    private readonly string s_updateClientClock = @"
        UPDATE tcat_workgroup_clients SET vectorClock = @VectorClock WHERE id = @Id";

    private readonly string s_deleteMediaItemFromWorkgroup = @"
        DELETE FROM tcat_workgroup_media WHERE media = @MediaId";

    /*----------------------------------------------------------------------------
        %%Function: OpenDatabase
        %%Qualified: Thetacat.Model.WorkgroupDb.OpenDatabase
    ----------------------------------------------------------------------------*/
    private SQLite OpenDatabase()
    {
        SQLite connection = SQLite.OpenConnection($"Data Source={m_database}");

        return connection;
    }

    /*----------------------------------------------------------------------------
        %%Function: CreateDatabase
        %%Qualified: Thetacat.Model.WorkgroupDb.CreateDatabase
    ----------------------------------------------------------------------------*/
    private SQLite CreateDatabase()
    {
        SQLiteConnection.CreateFile(m_database);

        ISql connection = OpenDatabase();

        connection.ExecuteNonQuery(s_createWorkgroupMedia);
        connection.ExecuteNonQuery(s_createWorkgroupFilters);
        connection.ExecuteNonQuery(s_createWorkgroupClients);
        connection.ExecuteNonQuery(s_createWorkgroupVClock);
        connection.ExecuteNonQuery(s_initializeVectorClock);
        connection.ExecuteNonQuery(s_initializeFilterVectorClock);

        connection.Close();

        return OpenDatabase();
    }

    /*----------------------------------------------------------------------------
        %%Function: Close
        %%Qualified: Thetacat.Model.WorkgroupDb.Close
    ----------------------------------------------------------------------------*/
    public void Close()
    {
        m_connection?.Close();
    }

    /*----------------------------------------------------------------------------
        %%Function: GetClientDetails
        %%Qualified: Thetacat.ServiceClient.LocalDatabase.WorkgroupDb.GetClientDetails
    ----------------------------------------------------------------------------*/
    public ServiceWorkgroupClient? GetClientDetails(string clientName)
    {
        try
        {
            ServiceWorkgroupClient client =
                _Connection.ExecuteDelegatedQuery(
                    Guid.NewGuid(),
                    s_queryWorkgroupClientDetailsByName,
                    (ISqlReader reader, Guid crids, ref ServiceWorkgroupClient _client) =>
                    {
                        _client.ClientId = reader.GetGuid(0);
                        _client.ClientName = reader.GetString(1);
                        _client.VectorClock = reader.GetInt32(2);
                    },
                    s_aliases,
                    cmd => { cmd.AddParameterWithValue("@Name", clientName); }
                    );

            if (client.ClientId == null)
                return null;

            return client;
        }
        catch (SqlExceptionNoResults)
        {
            return null;
        }
    }

    /*----------------------------------------------------------------------------
        %%Function: DeleteMediaItemFromWorkgroup
        %%Qualified: Thetacat.ServiceClient.LocalDatabase.WorkgroupDb.DeleteMediaItemFromWorkgroup
    ----------------------------------------------------------------------------*/
    public void DeleteMediaItemFromWorkgroup(Guid itemId)
    {
        _Connection.ExecuteNonQuery(
            s_deleteMediaItemFromWorkgroup,
            cmd => cmd.AddParameterWithValue("@MediaId", itemId.ToString()),
            s_aliases);
    }

    /*----------------------------------------------------------------------------
        %%Function: GetLatestWorkgroupMediaWithClock
        %%Qualified: Thetacat.ServiceClient.LocalDatabase.WorkgroupDb.GetLatestWorkgroupMediaWithClock
    ----------------------------------------------------------------------------*/
    public ServiceWorkgroupMediaClock GetLatestWorkgroupMediaWithClock()
    {
        // sqlite can't do multiple recordsets, so we have to wrap this in an exclusive
        // transaction
        return DoExclusiveDatabaseWork(
            () =>
            {
                try
                {
                    ServiceWorkgroupMediaClock mediaWithClock =
                        _Connection.ExecuteDelegatedQuery(
                            Guid.NewGuid(),
                            s_queryWorkgroupMediaClock,
                            (ISqlReader reader, Guid correlationId, ref ServiceWorkgroupMediaClock building) =>
                            {
                                ServiceWorkgroupItem item =
                                    new()
                                    {
                                        MediaId = reader.GetGuid(0),
                                        Path = reader.GetString(1),
                                        CachedBy = reader.GetGuid(2),
                                        CachedDate = reader.GetNullableDateTime(3),
                                        VectorClock = reader.GetInt32(4),
                                        MD5 = reader.GetString(5)
                                    };

                                building.Media ??= new List<ServiceWorkgroupItem>();
                                building.Media.Add(item);
                            },
                            s_aliases);

                    mediaWithClock.VectorClock = _Connection.NExecuteScalar(new SqlCommandTextInit(s_queryWorkgroupClock));
                    return mediaWithClock;
                }
                catch (SqlExceptionNoResults)
                {
                    return new ServiceWorkgroupMediaClock()
                    {
                        VectorClock = _Connection.NExecuteScalar(new SqlCommandTextInit(s_queryWorkgroupClock)),
                    };
                }
            });
    }

    /*----------------------------------------------------------------------------
        %%Function: GetLatestWorkgroupFilters
        %%Qualified: Thetacat.ServiceClient.LocalDatabase.WorkgroupDb.GetLatestWorkgroupFilters

        Get the complete set of filter definitions for this workgroup

    TODO: keep implementing WG filters. ALSO, update the workgroup database for cat-cache-localTest
    (each catalog has its own workgroup cache, so we don't have to worry about mixing them in the
    same workgroup db)
    ----------------------------------------------------------------------------*/
    public List<ServiceWorkgroupFilter> GetLatestWorkgroupFilters()
    {
        // sqlite can't do multiple recordsets, so we have to wrap this in an exclusive
        // transaction
        return DoExclusiveDatabaseWork(
            () =>
            {
                try
                {
                    List<ServiceWorkgroupFilter> filters =
                        _Connection.ExecuteDelegatedQuery(
                            Guid.NewGuid(),
                            s_queryWorkgroupFilters,
                            (ISqlReader reader, Guid correlationId, ref List<ServiceWorkgroupFilter> building) =>
                            {
                                ServiceWorkgroupFilter item =
                                    new()
                                    {
                                        Id = reader.GetGuid(0),
                                        Name = reader.GetString(1),
                                        Description = reader.GetString(2),
                                        Expression = reader.GetString(3),
                                        FilterClock = reader.GetInt32(4)
                                    };

                                building.Add(item);
                            },
                            s_aliases);

                    return filters;
                }
                catch (SqlExceptionNoResults)
                {
                    return new List<ServiceWorkgroupFilter>();
                }
            });
    }
    
    /*----------------------------------------------------------------------------
        %%Function: CreateWorkgroupClient
        %%Qualified: Thetacat.ServiceClient.LocalDatabase.WorkgroupDb.CreateWorkgroupClient
    ----------------------------------------------------------------------------*/
    public void CreateWorkgroupClient(ServiceWorkgroupClient client)
    {
        _Connection.ExecuteNonQuery(
            s_insertWorkgroupClient,
            (cmd) =>
            {
                cmd.AddParameterWithValue("@Id", client.ClientId.ToString());
                cmd.AddParameterWithValue("@Name", client.ClientName);
                cmd.AddParameterWithValue("@VectorClock", client.VectorClock);
            });
    }

    /*----------------------------------------------------------------------------
        %%Function: UpdateClientClock
        %%Qualified: Thetacat.ServiceClient.LocalDatabase.WorkgroupDb.UpdateClientClock
    ----------------------------------------------------------------------------*/
    public void UpdateClientClock(ServiceWorkgroupClient client, int newClock)
    {
        _Connection.ExecuteNonQuery(
            s_updateClientClock,
            (cmd) =>
            {
                cmd.AddParameterWithValue("@Id", client.ClientId.ToString());
                cmd.AddParameterWithValue("@VectorClock", newClock);
            });
    }

    /*----------------------------------------------------------------------------
        %%Function: DoExclusiveDatabaseWork
        %%Qualified: Thetacat.ServiceClient.LocalDatabase.WorkgroupDb.DoExclusiveDatabaseWork<T>
    ----------------------------------------------------------------------------*/
    T DoExclusiveDatabaseWork<T>(Func<T> work)
    {
        _Connection.BeginExclusiveTransaction();

        try
        {
            T t = work();
            _Connection.Commit();

            return t;
        }
        catch (Exception)
        {
            _Connection.Rollback();
            throw;
        }
    }

    public static void ExecutePartedCommands<T>(ISql sql, string commandBase, IEnumerable<T> items, Func<T, string> buildLine, int partLimit, string joinString, TableAliases? aliases = null)
    {
        StringBuilder sb = new StringBuilder();
        int current = 0;

        sb.Clear();
        sb.Append(commandBase);

        foreach (T item in items)
        {
            if (current == partLimit)
            {
                sql.ExecuteNonQuery(new SqlCommandTextInit(sb.ToString(), aliases));
                current = 0;
                sb.Clear();
                sb.Append(commandBase);
            }

            if (current > 0)
                sb.Append(joinString);

            sb.Append(buildLine(item));

            current++;
        }

        if (current > 0)
        {
            string sCmd = sb.ToString();

            if (!string.IsNullOrWhiteSpace(sCmd))
                sql.ExecuteNonQuery(new SqlCommandTextInit(sCmd, aliases));
        }
    }

    /*----------------------------------------------------------------------------
        %%Function: UpdateInsertCacheEntries
        %%Qualified: Thetacat.ServiceClient.LocalDatabase.WorkgroupDb.UpdateInsertCacheEntries
    ----------------------------------------------------------------------------*/
    public void UpdateInsertCacheEntries(
        int baseClock,
        Guid clientId,
        Dictionary<Guid, List<KeyValuePair<string, string>>> cacheChanges,
        List<WorkgroupCacheEntry> inserts)
    {
        List<string> updateLines = new();

        foreach (Guid key in cacheChanges.Keys)
        {
            List<KeyValuePair<string, string>> updates = cacheChanges[key];

            if (updates.Count == 0)
                continue;

            StringBuilder builder = new StringBuilder("UPDATE tcat_workgroup_media SET");
            bool first = true;
            foreach (KeyValuePair<string, string> value in updates)
            {
                if (first)
                {
                    builder.Append(" ");
                    first = false;
                }
                else
                {
                    builder.Append(", ");
                }

                builder.Append(value.Key);
                builder.Append("=");
                builder.Append(value.Value);
            }

            builder.Append($" WHERE media='{key.ToString()}'");
            updateLines.Add(builder.ToString());
        }

        DoExclusiveDatabaseWork(
            () =>
            {
                int currectVector = _Connection.NExecuteScalar(new SqlCommandTextInit(s_queryWorkgroupClock));

                if (currectVector != baseClock)
                    throw new CatExceptionDataCoherencyFailure();

                ExecutePartedCommands(
                    _Connection,
                    "",
                    updateLines,
                    (line) => line,
                    100,
                    ";",
                    null);

                ExecutePartedCommands(
                    _Connection,
                    "INSERT INTO tcat_workgroup_media (media, path, cachedBy, cachedDate, vectorClock, md5) VALUES ",
                    inserts,
                    (entry) =>
                        $"('{entry.ID.ToString()}', {SqlText.SqlifyQuoted(entry.Path.ToString())}, '{entry.CachedBy.ToString()}', {SqlText.Nullable(entry.CachedDate?.ToUniversalTime().ToString("u"))}, {SqlText.Nullable(entry.VectorClock)}, {SqlText.SqlifyQuoted(entry.MD5)}) ",
                    100,
                    ",",
                    null);

                // and lastly, update the vector clocks
                _Connection.ExecuteNonQuery(
                    new SqlCommandTextInit(s_updateWorkgroupClock),
                    (cmd) => cmd.AddParameterWithValue("@VectorClock", baseClock + 1));

                _Connection.ExecuteNonQuery(
                    new SqlCommandTextInit(s_updateClientClock),
                    (cmd) =>
                    {
                        cmd.AddParameterWithValue("@VectorClock", baseClock + 1);
                        cmd.AddParameterWithValue("@Id", clientId.ToString());
                    });

                return true;
            });
    }
}
