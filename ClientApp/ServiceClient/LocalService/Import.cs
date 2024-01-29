using System;
using System.Collections.Generic;
using System.Text;
using TCore;
using Thetacat.Import;
using Thetacat.Model;

namespace Thetacat.ServiceClient.LocalService;

public class Import
{
    private static Dictionary<string, string> s_aliases =
        new()
        {
            { "tcat_import", "IMP" },
        };

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
                SqlReader.DoGenericQueryDelegateRead(
                    sql,
                    crid,
                    sQuery,
                    (SqlReader reader, Guid correlationId, ref List<ServiceImportItem> building) =>
                    {
                        ServiceImportItem item =
                            new()
                            {
                                ID = reader.Reader.GetGuid(0),
                                State = reader.Reader.GetString(1),
                                SourcePath = reader.Reader.GetString(2),
                                SourceServer = reader.Reader.GetString(3),
                                UploadDate = !reader.Reader.IsDBNull(4) ? reader.Reader.GetDateTime(4) : null,
                                Source = !reader.Reader.IsDBNull(5) ? reader.Reader.GetString(5) : null
                            };

                        building.Add(item);
                    },
                    (cmd) => cmd.Parameters.AddWithValue("@SourceClient", sourceClient));

            return importItems;
        }
        catch (TcSqlExceptionNoResults)
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
                SqlReader.DoGenericQueryDelegateRead(
                    sql,
                    crid,
                    sQuery,
                    (SqlReader reader, Guid correlationId, ref List<ServiceImportItem> building) =>
                    {
                        ServiceImportItem item =
                            new()
                            {
                                ID = reader.Reader.GetGuid(0),
                                State = reader.Reader.GetString(1),
                                SourcePath = reader.Reader.GetString(2),
                                SourceServer = reader.Reader.GetString(3),
                                UploadDate = !reader.Reader.IsDBNull(4) ? reader.Reader.GetDateTime(4) : null,
                                Source = !reader.Reader.IsDBNull(5) ? reader.Reader.GetString(5) : null
                            };

                        building.Add(item);
                    });

            return importItems;
        }
        catch (TcSqlExceptionNoResults)
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
                    cmd.Parameters.AddWithValue("@MediaID", id);
                    cmd.Parameters.AddWithValue("@NewState", ImportItem.StringFromState(ImportItem.ImportState.Complete));
                });

            sql.ExecuteNonQuery(
                new SqlCommandTextInit(s_updateMediaState, s_aliases),
                (cmd) =>
                {
                    cmd.Parameters.AddWithValue("@MediaID", id);
                    cmd.Parameters.AddWithValue("@NewState", MediaItem.StringFromState(MediaItemState.Active));
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
                    cmd.Parameters.AddWithValue("@MediaID", id);
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
                    $"('{Sql.Sqlify(item.ID.ToString())}', '{ImportItem.StringFromState(item.State)}', '{Sql.Sqlify(item.SourcePath)}', '{Sql.Sqlify(item.SourceServer)}', {Sql.Nullable(item.Source)}) ");

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
