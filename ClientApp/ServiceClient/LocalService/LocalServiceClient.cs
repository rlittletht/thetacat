using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using TCore;
using Thetacat.Logging;
using Thetacat.Secrets;
using Thetacat.Types;

namespace Thetacat.ServiceClient.LocalService;

public delegate void LogDelegate(EventType eventType, string log, string? details = null, Guid? correlationId = null);

public class LocalServiceClient
{
    private static Sql? m_sql;
    public static LogDelegate? LogService { get; set; }

    public static Sql Sql
    {
        get
        {
            if (m_sql == null)
                throw new Exception("client not initialized");

            return m_sql;
        }
    }

    public static void EnsureConnected()
    {
        if (m_sql != null)
            return;

        if (String.IsNullOrWhiteSpace(AppSecrets.MasterSqlConnectionString))
            throw new CatExceptionNoSqlConnection();

        try
        {
            m_sql = Sql.OpenConnection(AppSecrets.MasterSqlConnectionString);
        }
        catch (Exception e)
        {
            throw new CatExceptionNoSqlConnection(Guid.NewGuid(), e, "failed to open SQL connection");
            throw;
        }
    }

    public delegate void DelegateReader<T>(SqlReader sqlr, Guid crid, ref T t);

    public static T? DoGenericQueryDelegateRead<T>(string sQuery, DelegateReader<T>? delegateReader)
    {
        Guid crid = Guid.NewGuid();
        LocalSqlHolder? lsh = null;
        SR sr = SR.Failed("unknown");

        try
        {
            lsh = new LocalSqlHolder(null, crid, AppSecrets.MasterSqlConnectionString);
            string sCmd = sQuery;

            if (delegateReader == null)
            {
                // just execute as a command
                Sql.ExecuteNonQuery(lsh, sCmd, AppSecrets.MasterSqlConnectionString);
                return default;
            }
            else
            {
                SqlReader sqlr = new SqlReader(lsh);
                try
                {
                    sqlr.ExecuteQuery(sQuery, AppSecrets.MasterSqlConnectionString);
                    sr.CorrelationID = crid;
                    T? t = default;
                    if (t == null)
                        throw new Exception("failed to create return class");
                    bool fOnce = false;

                    while (sqlr.Reader.Read())
                    {
                        delegateReader(sqlr, crid, ref t);
                        fOnce = true;
                    }

                    if (!fOnce)
                        throw new TcSqlExceptionNoResults(crid);

                    return t;

                }
                catch (Exception)
                {
                    sqlr.Close();
                    throw;
                }
            }
        }
        finally
        {
            lsh?.Close();
        }
    }

    public static T DoGenericQueryWithAliases<T>(
        string query, 
        Dictionary<string, string> aliases, 
        SqlReader.DelegateReader<T> delegateReader, 
        CustomizeCommandDelegate? custDelegate = null) where T : new()
    {
        Guid crid = Guid.NewGuid();

        SqlSelect selectTags = new SqlSelect();

        selectTags.AddBase(query);
        selectTags.AddAliases(aliases);

        string sQuery = selectTags.ToString();

        try
        {
            LocalServiceClient.EnsureConnected();

            T t =
                SqlReader.DoGenericQueryDelegateRead(
                    LocalServiceClient.Sql,
                    crid,
                    sQuery,
                    delegateReader, 
                    custDelegate);

            return t;
        }
        catch (TcSqlExceptionNoResults)
        {
            return new T();
        }
        catch (CatExceptionNoSqlConnection)
        {
            return new T();
        }
    }

    public static void DoGenericCommandWithAliases(string query, Dictionary<string, string> aliases, CustomizeCommandDelegate? custDelegate)
    {
        Guid crid = Guid.NewGuid();
        LocalServiceClient.EnsureConnected();

        SqlSelect selectTags = new SqlSelect();

        selectTags.AddBase(query);
        selectTags.AddAliases(aliases);

        string sQuery = selectTags.ToString();

        try
        {
            LocalServiceClient.Sql.ExecuteNonQuery(
                new SqlCommandTextInit(query, aliases),
                custDelegate);

            return;
        }
        catch (TcSqlExceptionNoResults)
        {
            return;
        }
    }
}
