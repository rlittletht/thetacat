using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
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

    public ClientDatabase(ISql sql)
    {
        m_database = new PathSegment("mock");
        m_connection = sql;
    }

    public ClientDatabase(PathSegment database)
    {
        m_database = database;

        if (!Path.Exists(m_database.Local))
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
}
