using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Data.SqlClient;
using TCore;
using TCore.PostfixText;
using TCore.SqlCore;
using TCore.SqlClient;
using Thetacat.Import;
using Thetacat.Model;
using Microsoft.VisualBasic;

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
        WHERE $$tcat_import$$.source = @SourceClient AND $$tcat_import$$.catalog_id = @CatalogID";

    private static readonly string s_deleteMediaItem = @"
        DELETE FROM tcat_import WHERE catalog_id=@CatalogID AND id=@MediaID";

    public static void DeleteMediaItem(Guid catalogId, Guid mediaId)
    {
        LocalServiceClient.DoGenericCommandWithAliases(
            s_deleteMediaItem,
            s_aliases,
            cmd =>
            {
                cmd.AddParameterWithValue("@CatalogID", catalogId);
                cmd.AddParameterWithValue("@MediaID", mediaId);
            });
    }

    public static List<ServiceImportItem> GetImportsForClient(Guid catalogID, string sourceClient)
    {
        return LocalServiceClient.DoGenericQueryWithAliases(
                s_baseQuery,
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
                s_aliases,
                (cmd) =>
                {
                    cmd.AddParameterWithValue("@SourceClient", sourceClient);
                    cmd.AddParameterWithValue("@CatalogID", catalogID);
                });
    }

    private static readonly string s_baseQueryAll = $@"
        SELECT 
            $$tcat_import$$.id, $$tcat_import$$.state, $$tcat_import$$.sourcePath, 
            $$tcat_import$$.sourceServer, $$tcat_import$$.uploadDate, $$tcat_import$$.source
        FROM $$#tcat_import$$
        WHERE $$tcat_import$$.catalog_id = @CatalogID";

    public static List<ServiceImportItem> GetAllImports(Guid catalogID)
    {
        return LocalServiceClient.DoGenericQueryWithAliases(
            s_baseQueryAll,
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
            s_aliases,
            cmd => cmd.AddParameterWithValue("@CatalogID", catalogID));
    }

    private static readonly string s_queryUpdateState = @"
        UPDATE tcat_import SET state=@NewState WHERE id=@MediaID AND catalog_id=@CatalogID";

    private static readonly string s_deleteImportItem = @"
        DELETE FROM tcat_import WHERE id=@MediaID AND catalog_id=@CatalogID";

    private static readonly string s_updateMediaState = @"
        UPDATE tcat_media SET state=@NewState WHERE id=@MediaID AND catalog_id=@CatalogID";


    public static void CompleteImportForItem(Guid catalogID, Guid id)
    {
        Guid crid = Guid.NewGuid();
        ISql sql = LocalServiceClient.GetConnection();

        sql.BeginTransaction();

        try
        {
            sql.ExecuteNonQuery(
                new SqlCommandTextInit(s_queryUpdateState, s_aliases),
                (cmd) =>
                {
                    cmd.AddParameterWithValue("@MediaID", id);
                    cmd.AddParameterWithValue("@NewState", ImportItem.StringFromState(ImportItem.ImportState.Complete));
                    cmd.AddParameterWithValue("@CatalogID", catalogID);
                });

            sql.ExecuteNonQuery(
                new SqlCommandTextInit(s_updateMediaState, s_aliases),
                (cmd) =>
                {
                    cmd.AddParameterWithValue("@MediaID", id);
                    cmd.AddParameterWithValue("@NewState", MediaItem.StringFromState(MediaItemState.Active));
                    cmd.AddParameterWithValue("@CatalogID", catalogID);
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

    public static void DeleteImportItem(Guid catalogID, Guid id)
    {
        Guid crid = Guid.NewGuid();
        ISql sql = LocalServiceClient.GetConnection();

        sql.BeginTransaction();

        try
        {
            sql.ExecuteNonQuery(
                new SqlCommandTextInit(s_deleteImportItem, s_aliases),
                (cmd) =>
                {
                    cmd.AddParameterWithValue("@MediaID", id);
                    cmd.AddParameterWithValue("@CatalogID", catalogID);
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
            (catalog_id, id, state, sourcePath, sourceServer, source)
        VALUES ";

    // same as insertImportItem but includes uploadDate
    private static readonly string s_queryInsertServiceImportItem = @"
        INSERT INTO tcat_import
            (catalog_id, id, state, sourcePath, sourceServer, source, uploadDate)
        VALUES ";

    public static void InsertImportItems(Guid catalogID, IEnumerable<ImportItem> items)
    {
        LocalServiceClient.DoGenericPartedCommands(
            s_queryInsertImportItem,
            items,
            (item) =>
                $"('{catalogID}', {SqlText.SqlifyQuoted(item.ID.ToString())}, '{ImportItem.StringFromState(item.State)}', {SqlText.SqlifyQuoted(item.SourcePath)}, {SqlText.SqlifyQuoted(item.SourceServer)}, {SqlText.Nullable(item.Source)}) ",
            1000,
            ",",
            s_aliases);
    }

    public static void InsertServiceImportItems(Guid catalogID, IEnumerable<ServiceImportItem> items)
    {
        LocalServiceClient.DoGenericPartedCommands(
            s_queryInsertServiceImportItem,
            items,
            (item) =>
                $"('{catalogID}', {SqlText.SqlifyQuoted(item.ID.ToString())}, {SqlText.SqlifyQuoted(item.State ?? string.Empty)}, {SqlText.SqlifyQuoted(item.SourcePath ?? string.Empty)}, {SqlText.SqlifyQuoted(item.SourceServer ?? string.Empty)}, {SqlText.Nullable(item.Source)}, {SqlText.Nullable(item.UploadDate)}) ",
            1000,
            ",",
            s_aliases);
    }

    private static readonly string s_baseForIds = $@"
        SELECT 
            $$tcat_import$$.id, $$tcat_import$$.state, $$tcat_import$$.sourcePath, 
            $$tcat_import$$.sourceServer, $$tcat_import$$.uploadDate, $$tcat_import$$.source
        FROM $$#tcat_import$$";

    private static readonly string s_queryForIds = $@"
        SELECT 
            $$tcat_import$$.id, $$tcat_import$$.state, $$tcat_import$$.sourcePath, 
            $$tcat_import$$.sourceServer, $$tcat_import$$.uploadDate, $$tcat_import$$.source
        FROM $$#tcat_import$$
        WHERE $$tcat_import$$.catalog_id = @CatalogID AND $$tcat_import$$.catalog_id in @Ids";

    public static List<ServiceImportItem> QueryImportedItems(Guid catalogID, IEnumerable<Guid> ids)
    {
        SqlSelect select = new SqlSelect(s_baseForIds, s_aliases.Aliases);

        select.Where.Add("$$tcat_import$$.catalog_id = @CatalogID", SqlWhere.Op.And);
        List<string> idStrings = new();
        foreach (Guid id in ids)
        {
            idStrings.Add($"'{id:D}'");
        }

        select.Where.Add($"$$tcat_import$$.id in ({string.Join(",", idStrings)})", SqlWhere.Op.And);

        return LocalServiceClient.DoGenericQueryWithAliases(
            select.ToString(),
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
            s_aliases,
            (cmd) =>
            {
                cmd.AddParameterWithValue("@CatalogID", catalogID);
//                cmd.AddParameterWithValue("@Ids", string.Join(",", idStrings));
            });
    }

}
