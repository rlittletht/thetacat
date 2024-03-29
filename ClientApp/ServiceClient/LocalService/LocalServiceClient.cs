﻿using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Data.SqlClient;
using TCore;
using TCore.SqlCore;
using TCore.SqlClient;
using Thetacat.Logging;
using Thetacat.Secrets;
using Thetacat.Types;

namespace Thetacat.ServiceClient.LocalService;

public delegate void LogDelegate(EventType eventType, string log, string? details = null, Guid? correlationId = null);

public class LocalServiceClient
{
    public static LogDelegate? LogService { get; set; }

    public static ISql GetConnection()
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

    // public delegate void DelegateReader<T>(ISqlReader sqlr, Guid crid, ref T t);

    public static T? DoGenericQueryDelegateRead<T>(string sQuery, ISqlReader.DelegateReader<T>? delegateReader) where T : new()
    {
        Guid crid = Guid.NewGuid();
        LocalSqlHolder? lsh = null;
        SR sr = SR.Failed("unknown");

        try
        {
            lsh = new LocalSqlHolder(null, crid, AppSecrets.MasterSqlConnectionString);

            if (delegateReader == null)
            {
                lsh.Sql.ExecuteNonQuery(sQuery);
                return default;
            }

            return lsh.Sql.ExecuteDelegatedQuery(crid, sQuery, delegateReader);
        }
        finally
        {
            lsh?.Close();
        }
    }

    public static T DoGenericQueryWithAliases<T>(
        string query, 
        TCore.SqlCore.ISqlReader.DelegateReader<T> delegateReader,
        TableAliases aliases,
        CustomizeCommandDelegate? custDelegate = null) where T : new()
    {
        Guid crid = Guid.NewGuid();

        SqlSelect selectTags = new SqlSelect();

        selectTags.AddBase(query);
        selectTags.AddAliases(aliases);

        string sQuery = selectTags.ToString();
        ISql? sql = null;

        try
        {
            sql = LocalServiceClient.GetConnection();

            T t =
                sql.ExecuteDelegatedQuery<T>(
                    crid,
                    sQuery,
                    delegateReader,
                    null,
                    custDelegate);

            return t;
        }
        catch (SqlExceptionNoResults)
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

    public static void DoGenericCommandWithAliases(string query, TableAliases? aliases, CustomizeCommandDelegate? custDelegate)
    {
        Guid crid = Guid.NewGuid();
        ISql sql = LocalServiceClient.GetConnection();

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
        catch (SqlExceptionNoResults)
        {
            return;
        }
        finally
        {
            sql.Close();
        }
    }


    public static string WrapSqlTransactionTryCatch(string tryBlock, string catchBlock)
    {
        string sql =
            @$"
                BEGIN TRANSACTION
                BEGIN TRY
                  {tryBlock}
                  COMMIT TRANSACTION
                END TRY
                BEGIN CATCH
                  {catchBlock}
                  ROLLBACK TRANSACTION
                END CATCH";

        return sql;
    }

    public static void ExecutePartedCommands<T>(
        ISql sql, string commandBase, IEnumerable<T> items, Func<T, string> buildLine, int partLimit, string joinString, TableAliases? aliases)
    {
        StringBuilder sb = new StringBuilder();
        int current = 0;

        sb.Clear();
        sb.Append(commandBase);

        foreach (T item in items)
        {
            if (current == partLimit)
            {
                string command = sb.ToString();

                if (!string.IsNullOrWhiteSpace(command))
                {
                    LocalServiceClient.LogService?.Invoke(EventType.Verbose, command);
                    sql.ExecuteNonQuery(new SqlCommandTextInit(sb.ToString(), aliases));
                    current = 0;
                }

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
            {
                LocalServiceClient.LogService?.Invoke(EventType.Verbose, sCmd);
                sql.ExecuteNonQuery(new SqlCommandTextInit(sCmd, aliases));
            }
        }
    }

    public static void DoGenericPartedCommands<T>(
        string commandBase, 
        IEnumerable<T> items, 
        Func<T, string> buildLine, 
        int partLimit, 
        string joinString, 
        TableAliases? aliases)
    {
        ISql sql = GetConnection();

        sql.BeginTransaction();

        try
        {
            ExecutePartedCommands(
                sql,
                commandBase,
                items,
                buildLine,
                partLimit,
                joinString,
                aliases);

            sql.Commit();
        }
        catch (Exception)
        {
            sql.Rollback();
            throw;
        }
        finally
        {
            sql.Close();
        }
    }
}
