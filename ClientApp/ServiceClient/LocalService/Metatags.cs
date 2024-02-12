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
        DELETE FROM tcat_schemaversions
        DELETE FROM tcat_metatags
        INSERT INTO tcat_schemaversions (metatag_schema_version) VALUES (0)";

    public static ServiceMetatagSchema GetMetatagSchema()
    {
        Guid crid = Guid.NewGuid();
        Sql sql = LocalServiceClient.GetConnection();

        SqlSelect selectTags = new SqlSelect();

        selectTags.AddBase(
            "SELECT $$tcat_metatags$$.id, $$tcat_metatags$$.parent, $$tcat_metatags$$.name, $$tcat_metatags$$.description, $$tcat_metatags$$.standard FROM $$#tcat_metatags$$");
        selectTags.AddAliases(s_aliases);

        string selectSchemaVersion = "select metatag_schema_version from tcat_schemaversions";

        string sQuery = $"{selectTags.ToString()} {selectSchemaVersion}";

        // we do both queries in the same command in order to get the matching schema version

        try
        {
            ServiceMetatagSchema schema =
                sql.DoGenericMultiSetQueryDelegateRead<ServiceMetatagSchema>(
                    crid,
                    sQuery,
                    (ISqlReader reader, Guid correlationId, int recordset, ref ServiceMetatagSchema schemaBuilding) =>
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
                    }
                );

            return schema;
        }
        catch (SqlExceptionNoResults)
        {
            return new ServiceMetatagSchema()
                   {
                       SchemaVersion = sql.NExecuteScalar(new SqlCommandTextInit(selectSchemaVersion)),
                   };
        }
        finally
        {
            sql.Close();
        }
    }

    static string BuildInsertSql(MetatagSchemaDiffOp diffOp)
    {
        string description = SqlText.Sqlify(diffOp.Metatag.Description);
        string name = SqlText.Sqlify(diffOp.Metatag.Name);
        string parent = SqlText.Nullable(diffOp.Metatag.Parent);
        string standard = SqlText.Sqlify(diffOp.Metatag.Standard);

        return "INSERT INTO tcat_metatags (Description, ID, Name, Parent, Standard) "
            + $"VALUES ('{description}', '{diffOp.ID.ToString()}', '{name}', {parent}, '{standard}') ";
    }

    static string BuildDeleteSql(MetatagSchemaDiffOp diffOp)
    {
        return $"DELETE FROM tcat_metatags WHERE ID = '{diffOp.ID}'";
    }

    static string BuildUpdateSql(MetatagSchemaDiffOp diffOp)
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
        return $"UPDATE tcat_metatags SET {setsSql} WHERE ID='{diffOp.ID.ToString()}'";
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

    static string BuildWrapSqlVersionCheckUpdate(int requiredSchemaVersion, string block)
    {
        string sql =
            $@"
                IF EXISTS 
                  ( SELECT 1
                      FROM tcat_schemaversions WITH (UPDLOCK, HOLDLOCK)
                      WHERE metatag_schema_version = {requiredSchemaVersion}
                  )
                BEGIN
                    {block}
                    UPDATE tcat_schemaversions SET metatag_schema_version={requiredSchemaVersion + 1}
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
    public static void UpdateMetatagSchema(MetatagSchemaDiff schemaDiff)
    {
        List<string> updates = new();

        foreach (MetatagSchemaDiffOp op in schemaDiff.Ops)
        {
            if (op.Action == MetatagSchemaDiffOp.ActionType.Insert)
                updates.Add(BuildInsertSql(op));
            else if (op.Action == MetatagSchemaDiffOp.ActionType.Delete)
                updates.Add(BuildDeleteSql(op));
            else if (op.Action == MetatagSchemaDiffOp.ActionType.Update)
                updates.Add(BuildUpdateSql(op));
        }

        // now build the boilerlate around the updates
        // (note that the CATCH block doesn't include the rollback -- that is included
        // automatically
        string cmd = WrapSqlTransactionTryCatch(
            BuildWrapSqlVersionCheckUpdate(
                schemaDiff.BaseSchemaVersion,
                string.Join("\n ", updates)),
            "select 0");

        Sql sql = LocalServiceClient.GetConnection();

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

    public static void ResetMetatagSchema()
    {
        LocalServiceClient.DoGenericCommandWithAliases(s_resetMetatagSchema, null, null);
    }
}
