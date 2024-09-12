using System;
using System.Collections.Generic;
using TCore.SqlCore;

namespace Thetacat.ServiceClient.LocalService;

public class CatalogDefinitions
{
    private static TableAliases s_aliases =
        new(
            new()
            {
                { "tcat_catalogs", "TC" },
            });

    public static readonly string s_queryAllCatalogs = @"
        SELECT $$tcat_catalogs$$.id, $$tcat_catalogs$$.name, $$tcat_catalogs$$.description
        FROM $$#tcat_catalogs$$";

    public static readonly string s_createCatalogDefinition = @"
        INSERT INTO tcat_catalogs (id, name, description) VALUES (@ID, @Name, @Description)
        INSERT INTO tcat_schemaversions (catalog_id, metatag_schema_version) VALUES (@ID, 0)";

    public static List<ServiceCatalogDefinition> GetCatalogDefinitions()
    {
        return LocalServiceClient.DoGenericQueryWithAliases(
            s_queryAllCatalogs,
            (ISqlReader reader, Guid crid, ref List<ServiceCatalogDefinition> building) =>
            {
                ServiceCatalogDefinition item =
                    new(
                        reader.GetGuid(0),
                        reader.GetString(1),
                        reader.GetString(2));

                building.Add(item);
            },
            s_aliases);
    }

    public static void AddCatalogDefinition(ServiceCatalogDefinition item)
    {
        LocalServiceClient.DoGenericCommandWithAliases(
            s_createCatalogDefinition,
            s_aliases,
            (cmd) =>
            {
                cmd.AddParameterWithValue("@ID", item.ID);
                cmd.AddParameterWithValue("@Name", item.Name);
                cmd.AddParameterWithValue("@Description", item.Description);
            });
    }
}
