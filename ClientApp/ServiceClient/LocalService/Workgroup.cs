﻿using System;
using System.Collections.Generic;
using TCore;
using TCore.SqlCore;
using TCore.SqlClient;

namespace Thetacat.ServiceClient.LocalService;

public class Workgroup
{
    private static readonly TableAliases s_aliases = new TableAliases(
        new()
        {
            { "tcat_workgroups", "WG" },
            { "tcat_workgroup_clients", "WGC" },
            { "tcat_workgroup_media", "WGM" }
        });

    static readonly string s_queryAllWorkgroups = @"
            SELECT $$tcat_workgroups$$.id, $$tcat_workgroups$$.name, $$tcat_workgroups$$.serverPath, $$tcat_workgroups$$.cacheRoot
            FROM $$#tcat_workgroups$$";

    static readonly string s_queryWorkgroup = @"
            SELECT $$tcat_workgroups$$.id, $$tcat_workgroups$$.name, $$tcat_workgroups$$.serverPath, $$tcat_workgroups$$.cacheRoot
            FROM $$#tcat_workgroups$$
            WHERE $$tcat_workgroups$$.id = @Id";

#if WG_ON_SQL
    static readonly string s_queryWorkgroupMedia = @"
            SELECT $$tcat_workgroup_media$$.workgroup, $$tcat_workgroup_media$$.media, $$tcat_workgroup_media$$.path, $$tcat_workgroup_media$$.cachedBy, $$tcat_workgroup_media$$.cachedDate, $$tcat_workgroup_clients$$.name, $$tcat_workgroup_clients$$.authID
            FROM $$#tcat_workgroup_media$$
            INNER JOIN $$#tcat_workgroup_clients$$ ON $$tcat_workgroup_media$$.cachedBy = $$tcat_workgroup_clients$$.id
            WHERE $$tcat_workgroup_media$$.media = @WorkgroupId";
#endif

    private static readonly string s_insertWorkgroup = @"
            INSERT INTO tcat_workgroups (id, name, serverPath, cacheRoot) VALUES (@Id, @Name, @ServerPath, @CacheRoot)";

    private static readonly string s_updateWorkgroup = @"
            UPDATE tcat_workgroups SET name=@Name, serverPath=@ServerPath, cacheRoot=@CacheRoot WHERE id=@Id";

    private static readonly string s_deleteWorkgroups = @"
            DELETE from tcat_workgroups";

    public static void DeleteAllWorkgroups()
    {
        LocalServiceClient.DoGenericCommandWithAliases(s_deleteWorkgroups, null, null);
    }

    public static List<ServiceWorkgroup> ReadWorkgroups()
    {
        return LocalServiceClient.DoGenericQueryWithAliases<List<ServiceWorkgroup>>(
            s_queryAllWorkgroups,
            (ISqlReader reader, Guid correlationId, ref List<ServiceWorkgroup> building) =>
            {
                ServiceWorkgroup workgroup =
                    new ServiceWorkgroup()
                    {
                        ID = reader.GetGuid(0),
                        Name = reader.GetString(1),
                        ServerPath = reader.GetString(2),
                        CacheRoot = reader.GetString(3)
                    };

                building.Add(workgroup);
            },
            s_aliases);
    }

#if WG_ON_SQL
    public static List<ServiceWorkgroupItemClient> ReadWorkgroupMedia(Guid id)
    {
        HashSet<Guid> mediaAdded = new();

        return LocalServiceClient.DoGenericQueryWithAliases<List<ServiceWorkgroupItemClient>>(
            s_queryWorkgroupMedia,
            s_aliases,
            (SqlReader reader, Guid correlationId, ref List<ServiceWorkgroupItemClient> building) =>
            {
                ServiceWorkgroupItemClient workgroupItemClient =
                    new ServiceWorkgroupItemClient()
                    {
                        Item = new ServiceWorkgroupItem()
                               {
                                   WorkgroupId = reader.Reader.GetGuid(0),
                                   MediaId = reader.Reader.GetGuid(1),
                                   Path = reader.Reader.GetString(2),
                                   CachedBy = reader.Reader.GetGuid(3),
                                   CachedDate = reader.Reader.GetDateTime(4),
                               },
                        Client = new ServiceWorkgroupClient()
                                 {
                                     ClientId = reader.Reader.GetGuid(3),
                                     ClientName = reader.Reader.GetString(5),
                                     AuthId = reader.Reader.IsDBNull(6) ? null : reader.Reader.GetGuid(6)
                                 }
                    };

                building.Add(workgroupItemClient);
            },
            (cmd) =>
            {
                cmd.Parameters.AddWithValue("@WorkgroupId", id);
            }
            );
    }
#endif

    public static ServiceWorkgroup GetWorkgroupDetails(Guid id)
    {
        return LocalServiceClient.DoGenericQueryWithAliases<ServiceWorkgroup>(
            s_queryWorkgroup,
            (ISqlReader reader, Guid correlationId, ref ServiceWorkgroup building) =>
            {
                building.ID = reader.GetGuid(0);
                building.Name = reader.GetString(1);
                building.ServerPath = reader.GetString(2);
                building.CacheRoot = reader.GetString(3);
            },
            s_aliases,
            (cmd) =>
            {
                cmd.AddParameterWithValue("@Id", id);
            });
    }

    public static void CreateWorkgroup(ServiceWorkgroup workgroup)
    {
        LocalServiceClient.DoGenericCommandWithAliases(
            s_insertWorkgroup,
            s_aliases,
            (cmd) =>
            {
                cmd.AddParameterWithValue("@Id", workgroup.ID);
                cmd.AddParameterWithValue("@Name", workgroup.Name);
                cmd.AddParameterWithValue("@ServerPath", workgroup.ServerPath);
                cmd.AddParameterWithValue("@CacheRoot", workgroup.CacheRoot);
            });
    }

    public static void UpdateWorkgroup(ServiceWorkgroup workgroup)
    {
        LocalServiceClient.DoGenericCommandWithAliases(
            s_updateWorkgroup,
            s_aliases,
            (cmd) =>
            {
                cmd.AddParameterWithValue("@Id", workgroup.ID);
                cmd.AddParameterWithValue("@Name", workgroup.Name);
                cmd.AddParameterWithValue("@ServerPath", workgroup.ServerPath);
                cmd.AddParameterWithValue("@CacheRoot", workgroup.CacheRoot);
            });
    }
}
