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

        SqlSelect select = new SqlSelect();

        select.AddBase("SELECT $$tcat_metatags$$.id, $$tcat_metatags$$.parent, $$tcat_metatags$$.name, $$tcat_metatags$$.description FROM $$#tcat_metatags$$");
        select.AddAliases(s_aliases);

        LocalServiceClient.Sql.BeginTransaction();

        ServiceMetatagSchema schema =
            SqlReader.DoGenericQueryDelegateRead<ServiceMetatagSchema>(
                LocalServiceClient.Sql,
                crid,
                select.ToString(),
                (SqlReader reader, Guid correlationId, ref ServiceMetatagSchema schemaBuilding) =>
                {
                    ServiceMetatag metatag = new()
                    {
                        ID = reader.Reader.GetGuid(0),
                        Parent = reader.Reader.IsDBNull(1) ? null : reader.Reader.GetGuid(1),
                        Name = reader.Reader.GetString(2),
                        Description = reader.Reader.GetString(3)
                    };

                    if (schemaBuilding.Metatags == null)
                        throw new Exception("metatags no preallocated");

                    schemaBuilding.Metatags.Add(metatag);
                }
            );

        ServiceMetatagSchemaVersion version =
            SqlReader.DoGenericQueryDelegateRead<ServiceMetatagSchemaVersion>(
                LocalServiceClient.Sql,
                crid,
                "SELECT metatag_schema_version FROM tcat_schemaversions",
                (SqlReader reader, Guid correlationid, ref ServiceMetatagSchemaVersion schemaVersion) =>
                {
                    schemaVersion.SchemaVersion = reader.Reader.GetInt32(0);
                });

        schema.SchemaVersion = version.SchemaVersion;

        LocalServiceClient.Sql.Commit();

        return schema;
    }
}
