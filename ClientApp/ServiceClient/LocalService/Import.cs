using System;
using System.Collections.Generic;
using System.Text;
using TCore;
using TCore.SqlCore;
using TCore.SqlClient;
using Thetacat.Import;
using Thetacat.Model;

namespace Thetacat.ServiceClient.LocalService;

public class Import
{
    private static TableAliases s_aliases =
        new(
            new()
            {
                { "tcat_import", "IMP" },
            });

    private static readonly string s_baseQuery = $@"
        SELECT 
            $$tcat_import$$.id, $$tcat_import$$.state, $$tcat_import$$.sourcePath, 
            $$tcat_import$$.sourceServer, $$tcat_import$$.uploadDate, $$tcat_import$$.source
        FROM $$#tcat_import$$
        WHERE $$tcat_import$$.source = @SourceClient";

    public static List<ServiceImportItem> GetPendingImportsForClient(string sourceClient)
    {
        Guid crid = Guid.NewGuid();
        Sql sql = LocalServiceClient.GetConnection();

        SqlSelect selectTags = new SqlSelect();

        selectTags.AddBase(s_baseQuery);
        selectTags.AddAliases(s_aliases);

        string sQuery = selectTags.ToString();

        try
        {
            List<ServiceImportItem> importItems =
                sql.DoGenericQueryDelegateRead(
                    crid,
                    sQuery,
                    (ISqlReader reader, Guid correlationId, ref List<ServiceImportItem> building) =>
                    {
                        ServiceImportItem item =
                            new()
                            {
                                ID = reader.GetGuid(0),
                                State = reader.GetString(1),
                                SourcePath = reader.GetString(2),
                                SourceServer = reader.GetString(3),
                                UploadDate = reader.GetNullableDateTime(4),
                                Source = reader.GetNullableString(5)
                            };

                        building.Add(item);
                    },
                    null,
                    (cmd) => cmd.AddParameterWithValue("@SourceClient", sourceClient));

            return importItems;
        }
        catch (SqlExceptionNoResults)
        {
            return new List<ServiceImportItem>();
        }
        finally
        {
            sql.Close();
        }
    }

    private static readonly string s_baseQueryAll = $@"
        SELECT 
            $$tcat_import$$.id, $$tcat_import$$.state, $$tcat_import$$.sourcePath, 
            $$tcat_import$$.sourceServer, $$tcat_import$$.uploadDate, $$tcat_import$$.source
        FROM $$#tcat_import$$";

    public static List<ServiceImportItem> GetAllImports()
    {
        Guid crid = Guid.NewGuid();
        Sql sql = LocalServiceClient.GetConnection();

        SqlSelect selectTags = new SqlSelect();

        selectTags.AddBase(s_baseQueryAll);
        selectTags.AddAliases(s_aliases);

        string sQuery = selectTags.ToString();

        try
        {
            List<ServiceImportItem> importItems =
                sql.DoGenericQueryDelegateRead(
                    crid,
                    sQuery,
                    (ISqlReader reader, Guid correlationId, ref List<ServiceImportItem> building) =>
                    {
                        ServiceImportItem item =
                            new()
                            {
                                ID = reader.GetGuid(0),
                                State = reader.GetString(1),
                                SourcePath = reader.GetString(2),
                                SourceServer = reader.GetString(3),
                                UploadDate = reader.GetNullableDateTime(4),
                                Source = reader.GetNullableString(5)
                            };

                        building.Add(item);
                    });

            return importItems;
        }
        catch (SqlExceptionNoResults)
        {
            return new List<ServiceImportItem>();
        }
        finally
        {
            sql.Close();
        }
    }

    private static readonly string s_queryUpdateState = @"
        UPDATE tcat_import SET state=@NewState WHERE id=@MediaID";

    private static readonly string s_deleteImportItem = @"
        DELETE FROM tcat_import WHERE id=@MediaID";

    private static readonly string s_updateMediaState = @"
        UPDATE tcat_media SET state=@NewState WHERE id=@MediaID";


    public static void CompleteImportForItem(Guid id)
    {
        Guid crid = Guid.NewGuid();
        Sql sql = LocalServiceClient.GetConnection();

        sql.BeginTransaction();

        try
        {
            sql.ExecuteNonQuery(
                new SqlCommandTextInit(s_queryUpdateState, s_aliases),
                (cmd) =>
                {
                    cmd.AddParameterWithValue("@MediaID", id);
                    cmd.AddParameterWithValue("@NewState", ImportItem.StringFromState(ImportItem.ImportState.Complete));
                });

            sql.ExecuteNonQuery(
                new SqlCommandTextInit(s_updateMediaState, s_aliases),
                (cmd) =>
                {
                    cmd.AddParameterWithValue("@MediaID", id);
                    cmd.AddParameterWithValue("@NewState", MediaItem.StringFromState(MediaItemState.Active));
                });

            sql.Commit();
        }
        catch
        {
            sql.Rollback();
            throw;
        }
        finally
        {
            sql.Close();
        }
    }

    public static void DeleteImportItem(Guid id)
    {
        Guid crid = Guid.NewGuid();
        Sql sql = LocalServiceClient.GetConnection();

        sql.BeginTransaction();

        try
        {
            sql.ExecuteNonQuery(
                new SqlCommandTextInit(s_deleteImportItem, s_aliases),
                (cmd) =>
                {
                    cmd.AddParameterWithValue("@MediaID", id);
                });

            sql.Commit();
        }
        catch
        {
            sql.Rollback();
            throw;
        }
        finally
        {
            sql.Close();
        }
    }

    private static readonly string s_queryInsertImportItem = @"
        INSERT INTO tcat_import
            (id, state, sourcePath, sourceServer, source)
        VALUES ";

    public static void InsertImportItems(IEnumerable<ImportItem> items)
    {
        Guid crid = Guid.NewGuid();
        Sql sql = LocalServiceClient.GetConnection();
        StringBuilder sb = new StringBuilder();

        sql.BeginTransaction();

        try
        {
            int current = 0;

            sb.Clear();
            sb.Append(s_queryInsertImportItem);

            foreach (ImportItem item in items)
            {
                if (current == 1000)
                {
                    sql.ExecuteNonQuery(new SqlCommandTextInit(sb.ToString(), s_aliases));
                    current = 0;
                    sb.Clear();
                    sb.Append(s_queryInsertImportItem);
                }

                if (current > 0)
                    sb.Append(",");

                sb.Append(
                    $"({SqlText.SqlifyQuoted(item.ID.ToString())}, '{ImportItem.StringFromState(item.State)}', {SqlText.SqlifyQuoted(item.SourcePath)}, {SqlText.SqlifyQuoted(item.SourceServer)}, {SqlText.Nullable(item.Source)}) ");

                current++;
            }

            if (current > 0)
                sql.ExecuteNonQuery(new SqlCommandTextInit(sb.ToString(), s_aliases));

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
