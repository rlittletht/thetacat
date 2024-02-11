using System;
using System.Collections.Generic;
using TCore;
using Thetacat.Logging;
using Thetacat.Secrets;
using Thetacat.Types;

namespace Thetacat.ServiceClient.LocalService;

public delegate void LogDelegate(EventType eventType, string log, string? details = null, Guid? correlationId = null);

public class LocalServiceClient
{
    public static LogDelegate? LogService { get; set; }

    public static Sql GetConnection()
    {
        if (String.IsNullOrWhiteSpace(AppSecrets.MasterSqlConnectionString))
            throw new CatExceptionNoSqlConnection();

        try
        {
            return Sql.OpenConnection(AppSecrets.MasterSqlConnectionString);
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
        Sql? sql = null;

        try
        {
            sql = LocalServiceClient.GetConnection();

            T t =
                SqlReader.DoGenericQueryDelegateRead(
                    sql,
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
        finally
        {
            sql?.Close();
        }
    }

    public static void DoGenericCommandWithAliases(string query, Dictionary<string, string>? aliases, CustomizeCommandDelegate? custDelegate)
    {
        Guid crid = Guid.NewGuid();
        Sql sql = LocalServiceClient.GetConnection();

        SqlSelect selectTags = new SqlSelect();

        selectTags.AddBase(query);
        if (aliases != null)
            selectTags.AddAliases(aliases);

        string sQuery = selectTags.ToString();

        try
        {
            sql.ExecuteNonQuery(
                new SqlCommandTextInit(query, aliases),
                custDelegate);

            return;
        }
        catch (TcSqlExceptionNoResults)
        {
            return;
        }
        finally
        {
            sql.Close();
        }
    }
}
