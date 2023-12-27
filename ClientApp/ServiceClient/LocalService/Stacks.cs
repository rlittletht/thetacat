using System;
using System.Collections.Generic;
using TCore;
using Thetacat.Types;

namespace Thetacat.ServiceClient.LocalService;

public class Stacks
{
    private static readonly Dictionary<string, string> s_aliases =
        new()
        {
            { "tcat_stackmedia", "SM" },
            { "tcat_stacks", "ST" },
        };

    private static readonly string s_queryAllStacks = @"
        SELECT $$tcat_stackmedia$$.id, $$tcat_stackmedia$$.media_id, $$tcat_stackmedia$$.orderHint,
                $$tcat_stacks$$.stackType, $$tcat_stacks$$.description
        FROM $$#tcat_stacksmedia$$
        INNER JOIN $$#tcat_stacks$$
            ON $$tcat_stacks$$.id = $$tcat_stacks$$.id";

    public static List<ServiceStack> GetAllStacks()
    {
        Guid crid = Guid.NewGuid();
        LocalServiceClient.EnsureConnected();

        SqlSelect selectTags = new SqlSelect();

        selectTags.AddBase(s_queryAllStacks);
        selectTags.AddAliases(s_aliases);

        string sQuery = selectTags.ToString();
        Dictionary<Guid, ServiceStack> mapStack = new Dictionary<Guid, ServiceStack>();

        try
        {
            List<ServiceStack> stacks =
                SqlReader.DoGenericQueryDelegateRead(
                    LocalServiceClient.Sql,
                    crid,
                    sQuery,
                    (SqlReader reader, Guid correlationId, ref List<ServiceStack> building) =>
                    {
                        Guid id = reader.Reader.GetGuid(0);

                        if (!mapStack.TryGetValue(id, out ServiceStack? existing))
                        {
                            existing =
                                new ServiceStack()
                                {
                                    Id = id,
                                    StackType = reader.Reader.GetString(3),
                                    Description = reader.Reader.GetString(4),
                                    StackItems = new List<ServiceStackItem>()
                                };
                            mapStack.Add(id, existing);
                            building.Add(existing);
                        }

                        ServiceStackItem item =
                            new()
                            {
                                MediaId = reader.Reader.GetGuid(1),
                                OrderHint = reader.Reader.GetInt32(2)
                            };

                        if (existing.StackItems == null)
                            throw new CatExceptionInternalFailure("stackitems wasn't preallocated");

                        existing.StackItems.Add(item);
                    });

            return stacks;
        }
        catch (TcSqlExceptionNoResults)
        {
            return new List<ServiceStack>();
        }
    }
}
