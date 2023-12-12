﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using TCore;
using Thetacat.Secrets;

namespace Thetacat.ServiceClient.LocalService;

public class LocalServiceClient
{
    private static Sql? m_sql;

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

        m_sql = Sql.OpenConnection(AppSecrets.MasterSqlConnectionString);
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
}
