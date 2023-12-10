using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Documents.DocumentStructures;
using TCore;
using Thetacat.Import;

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
            $$tcat_import.id, $$tcat_import.state, $$tcat_import.sourcePath, 
            $$tcat_import.sourceServer, $$tcat_import.uploadDate, $$tcat_import.source,
        FROM $$#$$tcat_import
        WHERE source = @SourceClient'";

    public static List<ServiceImportItem> GetPendingImportsForClient(string sourceClient)
    {
        Guid crid = Guid.NewGuid();
        LocalServiceClient.EnsureConnected();

        SqlSelect selectTags = new SqlSelect();

        selectTags.AddBase(s_baseQuery);
        selectTags.AddAliases(s_aliases);

        string sQuery = selectTags.ToString();

        try
        {
            List<ServiceImportItem> importItems =
                SqlReader.DoGenericQueryDelegateRead(
                    LocalServiceClient.Sql,
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
    }

    private static readonly string s_queryUpdateState = @"
        UPDATE tcat_import SET state=@NewState WHERE id=@MediaID";

    public static void UpdateImportStateForItem(Guid id, ImportItem item, ImportItem.ImportState newState)
    {
        Guid crid = Guid.NewGuid();
        LocalServiceClient.EnsureConnected();

        LocalServiceClient.Sql.ExecuteNonQuery(
            new SqlCommandTextInit(s_queryUpdateState, s_aliases),
            (cmd) =>
            {
                cmd.Parameters.AddWithValue("@MediaID", id);
                cmd.Parameters.AddWithValue("@NewState", ImportItem.StringFromState(newState));
            });
    }

    private static readonly string s_queryInsertImportItem = @"
        INSERT INTO tcat_import
            (id, state, sourcePath, sourceServer, source)
        VALUES ";

    public static void InsertImportItems(IEnumerable<ImportItem> items)
    {
        Guid crid = Guid.NewGuid();
        LocalServiceClient.EnsureConnected();
        StringBuilder sb = new StringBuilder();

        LocalServiceClient.Sql.BeginTransaction();

        try
        {
            int current = 0;

            sb.Clear();
            sb.Append(s_queryInsertImportItem);

            foreach (ImportItem item in items)
            {
                if (current == 1000)
                {
                    LocalServiceClient.Sql.ExecuteNonQuery(new SqlCommandTextInit(sb.ToString(), s_aliases));
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
                LocalServiceClient.Sql.ExecuteNonQuery(new SqlCommandTextInit(sb.ToString(), s_aliases));
        }
        catch (Exception)
        {
            LocalServiceClient.Sql.Rollback();
            throw;
        }

        LocalServiceClient.Sql.Commit();
    }
}
