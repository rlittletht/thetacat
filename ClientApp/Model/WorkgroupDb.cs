using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Windows;
using Thetacat.Migration.Elements.Metadata.UI;
using Thetacat.ServiceClient;
using Thetacat.TCore.TcSqlLite;
using Thetacat.Types;
using Thetacat.Util;

namespace Thetacat.Model;

public class WorkgroupDb
{
    private static Dictionary<string, string> s_aliases =
        new()
        {
            { "tcat_workgroup_media", "TWM" },
            { "tcat_workgroup_clients", "TWC" }
        };

    private PathSegment m_database;
    private SQLite? m_connection;
    // private SQLiteConnection? m_connection;

    private SQLite _Connection
    {
        get
        {
            if (m_connection == null)
                throw new CatExceptionInitializationFailure();

            return m_connection;
        }
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
        CREATE TABLE tcat_workgroup_media(media VARCHAR(36), path VARCHAR(1024), cachedBy VARCHAR(36), cachedDate NVARCHAR(64))";

    private readonly string s_createWorkgroupVClock = @"
        CREATE TABLE tcat_workgroup_vectorclock(vclock INTEGER)";

    private readonly string s_createWorkgroupClients = @"
        CREATE TABLE tcat_workgroup_clients(id VARCHAR(36), name VARCHAR(128), vectorClock INTEGER)";

    private readonly string s_initializeVectorClock = @"
        UPDATE tcat_workgroup_vectorclock SET vclock=0";

    private readonly string s_queryWorkgroupClientDetailsByName = @"
        SELECT $$tcat_workgroup_clients$$.id, $$tcat_workgroup_clients$$.name, $$tcat_workgroup_clients$$.vectorClock
        FROM $$#tcat_workgroup_clients$$
        WHERE $$tcat_workgroup_clients$$.name = @Name";

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

        SQLite connection = OpenDatabase();

        using SQLiteCommand cmd = connection.CreateCommand();

        connection.ExecuteNonQuery(s_createWorkgroupMedia);
        connection.ExecuteNonQuery(s_createWorkgroupClients);
        connection.ExecuteNonQuery(s_createWorkgroupVClock);
        connection.ExecuteNonQuery(s_initializeVectorClock);

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

    public ServiceWorkgroupClient GetClientDetails(string clientName)
    {
        ServiceWorkgroupClient client =
            SQLiteReader.DoGenericQueryDelegateRead(
                _Connection,
                Guid.NewGuid(),
                s_queryWorkgroupClientDetailsByName,
                (SQLiteReader reader, Guid crids, ref ServiceWorkgroupClient _client) =>
                {
                    _client.ClientId = reader.GetGuid(0);
                    _client.ClientName = reader.GetString(1);
                    _client.VectorClock = reader.GetInt32(2);
                },
                (cmd) =>
                {
                    cmd.Parameters.AddWithValue("@Name", clientName);
                });

        return client;
    }
}
