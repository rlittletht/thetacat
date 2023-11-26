using System;
using System.Collections.Generic;
using TCore;

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

        selectTags.AddBase("SELECT $$tcat_metatags$$.id, $$tcat_metatags$$.parent, $$tcat_metatags$$.name, $$tcat_metatags$$.description FROM $$#tcat_metatags$$");
        selectTags.AddAliases(s_aliases);

        string selectSchemaVersion = "select metatag_schema_version from tcat_schemaversions";

        string sQuery = $"{selectTags.ToString()} {selectSchemaVersion}";

        // we do both queries in the same command in order to get the matching schema version

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
                            Description = reader.Reader.GetString(3)
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
}
