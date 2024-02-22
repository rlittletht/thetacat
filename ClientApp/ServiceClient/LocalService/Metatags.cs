using System;
using System.Collections.Generic;
using System.Windows;
using TCore;
using TCore.SqlCore;
using TCore.SqlClient;
using Thetacat.Metatags.Model;

namespace Thetacat.ServiceClient.LocalService;

public class Metatags
{
    private static readonly TableAliases s_aliases =
        new(
            new()
            {
                { "tcat_metatags", "META" },
                { "tcat_schemaversions", "SV" }
            });

    private static readonly string s_resetMetatagSchema = @"
        DELETE FROM tcat_schemaversions WHERE catalog_id = @CatalogID
        DELETE FROM tcat_metatags WHERE catalog_id = @CatalogID
        INSERT INTO tcat_schemaversions (catalog_id, metatag_schema_version) VALUES (@CatalogID, 0)";

    private static readonly string s_getMetatagSchema = @"
        SELECT 
            $$tcat_metatags$$.id, $$tcat_metatags$$.parent, $$tcat_metatags$$.name, 
            $$tcat_metatags$$.description, $$tcat_metatags$$.standard 
        FROM $$#tcat_metatags$$
        WHERE $$tcat_metatags$$.catalog_id=@CatalogID";

    private static readonly string s_selectSchemaVersionBase = @"
        SELECT $$tcat_schemaversions$$.metatag_schema_version 
        FROM $$#tcat_schemaversions$$";

    public static ServiceMetatagSchema GetMetatagSchema(Guid catalogID)
    {
        Guid crid = Guid.NewGuid();
        ISql sql = LocalServiceClient.GetConnection();

        string sSelectSchemaVersion = $"{s_selectSchemaVersionBase} WHERE $$tcat_schemaversions$$.catalog_id='{catalogID}'";
        string sQuery = $"{s_getMetatagSchema} {sSelectSchemaVersion}";

        // we do both queries in the same command in order to get the matching schema version

        try
        {
            ServiceMetatagSchema schema =
                sql.ExecuteMultiSetDelegatedQuery(
                    crid,
                    sQuery,
                    (ISqlReader reader, Guid _, int recordset, ref ServiceMetatagSchema schemaBuilding) =>
                    {
                        if (recordset == 0)
                        {
                            ServiceMetatag metatag = new()
                                                     {
                                                         ID = reader.GetGuid(0),
                                                         Parent = reader.GetNullableGuid(1),
                                                         Name = reader.GetString(2),
                                                         Description = reader.GetString(3),
                                                         Standard = reader.GetString(4)
                                                     };

                            if (schemaBuilding.Metatags == null)
                                throw new Exception("metatags no preallocated");

                            schemaBuilding.Metatags.Add(metatag);
                        }
                        else if (recordset == 1)
                        {
                            schemaBuilding.SchemaVersion = reader.GetInt32(0);
                        }
                    },
                    s_aliases,
                    cmd=>cmd.AddParameterWithValue("@CatalogID", catalogID));

            return schema;
        }
        catch (SqlExceptionNoResults)
        {
            return new ServiceMetatagSchema()
                   {
                       SchemaVersion = 
                           sql.NExecuteScalar(
                               new SqlCommandTextInit(sSelectSchemaVersion, s_aliases)),
                   };
        }
        finally
        {
            sql.Close();
        }
    }

    static string BuildInsertSql(Guid catalogID, MetatagSchemaDiffOp diffOp)
    {
        string description = SqlText.Sqlify(diffOp.Metatag.Description);
        string name = SqlText.Sqlify(diffOp.Metatag.Name);
        string parent = SqlText.Nullable(diffOp.Metatag.Parent);
        string standard = SqlText.Sqlify(diffOp.Metatag.Standard);

        return "INSERT INTO tcat_metatags (catalog_id, Description, ID, Name, Parent, Standard) "
            + $"VALUES ('{catalogID}', '{description}', '{diffOp.ID.ToString()}', '{name}', {parent}, '{standard}') ";
    }

    static string BuildDeleteSql(Guid catalogID, MetatagSchemaDiffOp diffOp)
    {
        return $"DELETE FROM tcat_metatags WHERE ID = '{diffOp.ID}' AND catalog_id='{catalogID}'";
    }

    static string BuildUpdateSql(Guid catalogID, MetatagSchemaDiffOp diffOp)
    {
        List<string> sets = new();

        if (diffOp.IsNameChanged)
            sets.Add($"Name={SqlText.SqlifyQuoted(diffOp.Metatag.Name)}");
        if (diffOp.IsDescriptionChanged)
            sets.Add($"Description={SqlText.SqlifyQuoted(diffOp.Metatag.Description)}");
        if (diffOp.IsParentChanged)
            sets.Add($"Parent={SqlText.Nullable(diffOp.Metatag.Parent)}");
        if (diffOp.IsStandardChanged)
            sets.Add($"Standard={SqlText.Nullable(diffOp.Metatag.Standard)}");

        if (sets.Count == 0)
            return "";

        string setsSql = string.Join(", ", sets.ToArray());
        return $"UPDATE tcat_metatags SET {setsSql} WHERE ID='{diffOp.ID}' AND catalog_id='{catalogID}'";
    }

    static string WrapSqlTransactionTryCatch(string tryBlock, string catchBlock)
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

    static string BuildWrapSqlVersionCheckUpdate(Guid catalogID, int requiredSchemaVersion, string block)
    {
        string sql =
            $@"
                IF EXISTS 
                  ( SELECT 1
                      FROM tcat_schemaversions WITH (UPDLOCK, HOLDLOCK)
                      WHERE metatag_schema_version = {requiredSchemaVersion} AND catalog_id = '{catalogID}'
                  )
                BEGIN
                    {block}
                    UPDATE tcat_schemaversions SET metatag_schema_version={requiredSchemaVersion + 1} WHERE catalog_id='{catalogID}'
                    SELECT 1
                END
                ELSE
                BEGIN
                    SELECT 0
                END";

        return sql;
    }

    /*----------------------------------------------------------------------------
        %%Function: UpdateMetatagSchema
        %%Qualified: Thetacat.ServiceClient.LocalService.Metatags.UpdateMetatagSchema

        We have to craft a query that will update all the rows ONLY if the schema
        version is what we expect it to be.
    ----------------------------------------------------------------------------*/
    public static void UpdateMetatagSchema(Guid catalogID, MetatagSchemaDiff schemaDiff)
    {
        List<string> updates = new();

        foreach (MetatagSchemaDiffOp op in schemaDiff.Ops)
        {
            if (op.Action == MetatagSchemaDiffOp.ActionType.Insert)
                updates.Add(BuildInsertSql(catalogID, op));
            else if (op.Action == MetatagSchemaDiffOp.ActionType.Delete)
                updates.Add(BuildDeleteSql(catalogID, op));
            else if (op.Action == MetatagSchemaDiffOp.ActionType.Update)
                updates.Add(BuildUpdateSql(catalogID, op));
        }

        // now build the boilerlate around the updates
        // (note that the CATCH block doesn't include the rollback -- that is included
        // automatically
        string cmd = WrapSqlTransactionTryCatch(
            BuildWrapSqlVersionCheckUpdate(
                catalogID,
                schemaDiff.BaseSchemaVersion,
                string.Join("\n ", updates)),
            "select 0");

        ISql sql = LocalServiceClient.GetConnection();

        try
        {
            int result = sql.NExecuteScalar(new SqlCommandTextInit(cmd));

            if (result == 0)
            {
                MessageBox.Show("Failed to update schema");
                return;
            }
        }
        finally
        {
            sql.Close();
        }
    }

    public static void ResetMetatagSchema(Guid catalogID)
    {
        LocalServiceClient.DoGenericCommandWithAliases(
            s_resetMetatagSchema,
            null,
            cmd => cmd.AddParameterWithValue("@CatalogID", catalogID));
    }
}
