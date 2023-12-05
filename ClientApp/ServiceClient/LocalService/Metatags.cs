using System;
using System.Collections.Generic;
using System.Windows;
using TCore;
using Thetacat.Model;

namespace Thetacat.ServiceClient.LocalService;

public class Metatags
{
    private static Dictionary<string, string> s_aliases =
        new()
        {
            { "tcat_metatags", "META" },
        };

    public static ServiceMetatagSchema GetMetatagSchema()
    {
        Guid crid = Guid.NewGuid();
        LocalServiceClient.EnsureConnected();

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
                SqlReader.DoGenericMultiSetQueryDelegateRead<ServiceMetatagSchema>(
                    LocalServiceClient.Sql,
                    crid,
                    sQuery,
                    (SqlReader reader, Guid correlationId, int recordset, ref ServiceMetatagSchema schemaBuilding) =>
                    {
                        if (recordset == 0)
                        {
                            ServiceMetatag metatag = new()
                                                     {
                                                         ID = reader.Reader.GetGuid(0),
                                                         Parent = reader.Reader.IsDBNull(1)
                                                             ? null
                                                             : reader.Reader.GetGuid(1),
                                                         Name = reader.Reader.GetString(2),
                                                         Description = reader.Reader.GetString(3),
                                                         Standard = reader.Reader.GetString(4)
                                                     };

                            if (schemaBuilding.Metatags == null)
                                throw new Exception("metatags no preallocated");

                            schemaBuilding.Metatags.Add(metatag);
                        }
                        else if (recordset == 1)
                        {
                            schemaBuilding.SchemaVersion = reader.Reader.GetInt32(0);
                        }
                    }
                );

            return schema;
        }
        catch (TcSqlExceptionNoResults)
        {
            return new ServiceMetatagSchema()
                   {
                       SchemaVersion = LocalServiceClient.Sql.NExecuteScalar(new SqlCommandTextInit(selectSchemaVersion)),
                   };
        }
    }

    static string BuildInsertSql(MetatagSchemaDiffOp diffOp)
    {
        string description = TCore.Sql.Sqlify(diffOp.Metatag.Description);
        string name = TCore.Sql.Sqlify(diffOp.Metatag.Name);
        string parent = TCore.Sql.Nullable(diffOp.Metatag.Parent);
        string standard = TCore.Sql.Sqlify(diffOp.Metatag.Standard);

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
            sets.Add($"Name='{TCore.Sql.Sqlify(diffOp.Metatag.Name)}'");
        if (diffOp.IsDescriptionChanged)
            sets.Add($"Description='{TCore.Sql.Sqlify(diffOp.Metatag.Description)}'");
        if (diffOp.IsParentChanged)
            sets.Add($"Parent={TCore.Sql.Nullable(diffOp.Metatag.Parent)}");
        if (diffOp.IsStandardChanged)
            sets.Add($"Standard={TCore.Sql.Nullable(diffOp.Metatag.Standard)}");

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
        string sql = WrapSqlTransactionTryCatch(
            BuildWrapSqlVersionCheckUpdate(
                schemaDiff.BaseSchemaVersion,
                string.Join("\n ", updates)),
            "select 0");

        int result = LocalServiceClient.Sql.NExecuteScalar(new SqlCommandTextInit(sql));

        if (result == 0)
        {
            MessageBox.Show("Failed to update schema");
            return;
        }
    }
}
