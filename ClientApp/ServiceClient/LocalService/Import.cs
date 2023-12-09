using System;
using System.Collections.Generic;
using System.Windows.Documents.DocumentStructures;
using TCore;

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

}
