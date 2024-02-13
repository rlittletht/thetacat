using System;
using System.Collections.Generic;
using TCore;
using TCore.SqlCore;
using TCore.SqlClient;
using Thetacat.Model;
using Thetacat.Types;

namespace Thetacat.ServiceClient.LocalService;

public class Stacks
{
    private static readonly TableAliases s_aliases =
        new(
            new()
            {
                { "tcat_stackmedia", "SM" },
                { "tcat_stacks", "ST" },
            });

    private static readonly string s_queryAllStacks = @"
        SELECT $$tcat_stackmedia$$.id, $$tcat_stackmedia$$.media_id, $$tcat_stackmedia$$.orderHint,
                $$tcat_stacks$$.stackType, $$tcat_stacks$$.description
        FROM $$#tcat_stackmedia$$
        INNER JOIN $$#tcat_stacks$$
            ON $$tcat_stacks$$.id = $$tcat_stackmedia$$.id";

    public static List<ServiceStack> GetAllStacks()
    {
        Guid crid = Guid.NewGuid();
        ISql sql = LocalServiceClient.GetConnection();

        SqlSelect selectTags = new SqlSelect();

        selectTags.AddBase(s_queryAllStacks);
        selectTags.AddAliases(s_aliases);

        string sQuery = selectTags.ToString();
        Dictionary<Guid, ServiceStack> mapStack = new Dictionary<Guid, ServiceStack>();

        try
        {
            List<ServiceStack> stacks =
                sql.ExecuteDelegatedQuery(
                    crid,
                    sQuery,
                    (ISqlReader reader, Guid correlationId, ref List<ServiceStack> building) =>
                    {
                        Guid id = reader.GetGuid(0);

                        if (!mapStack.TryGetValue(id, out ServiceStack? existing))
                        {
                            existing =
                                new ServiceStack()
                                {
                                    Id = id,
                                    StackType = reader.GetString(3),
                                    Description = reader.GetString(4),
                                    StackItems = new List<ServiceStackItem>()
                                };
                            mapStack.Add(id, existing);
                            building.Add(existing);
                        }

                        ServiceStackItem item =
                            new()
                            {
                                MediaId = reader.GetGuid(1),
                                OrderHint = reader.GetInt32(2)
                            };

                        if (existing.StackItems == null)
                            throw new CatExceptionInternalFailure("stackitems wasn't preallocated");

                        existing.StackItems.Add(item);
                    });

            return stacks;
        }
        catch (SqlExceptionNoResults)
        {
            return new List<ServiceStack>();
        }
        finally
        {
            sql.Close();
        }
    }

    public static void AddInsertStackMediaToCommands(MediaStackDiff diff, List<string> updates)
    {
        foreach (MediaStackItem item in diff.Stack.Items)
        {
            updates.Add(
                $"INSERT INTO tcat_stackmedia (id, media_id, orderHint) VALUES ('{diff.Stack.StackId.ToString()}', '{item.MediaId.ToString()}', {item.StackIndex})");
        }
    }

    public static IEnumerable<string> AddUpdatesForDiff(MediaStackDiff diff, List<string> updates)
    {
        switch (diff.PendingOp)
        {
            case MediaStack.Op.Create:
                updates.Add($"INSERT INTO tcat_stacks (id, stackType, description) VALUES ('{diff.Stack.StackId.ToString()}', {SqlText.SqlifyQuoted(diff.Stack.Type)}, {SqlText.SqlifyQuoted(diff.Stack.Description)})");
                AddInsertStackMediaToCommands(diff, updates);
                return updates;
            case MediaStack.Op.Delete:
                updates.Add($"DELETE FROM tcat_stacks WHERE id='{diff.Stack.StackId.ToString()}");
                updates.Add($"DELETE FROM tcat_stackmedia WHERE id='{diff.Stack.StackId.ToString()}");
                return updates;
            case MediaStack.Op.Update:
                updates.Add($"UPDATE tcat_stacks SET description={SqlText.SqlifyQuoted(diff.Stack.Description)} WHERE id='{diff.Stack.StackId.ToString()}'");
                updates.Add($"DELETE FROM tcat_stackmedia WHERE id='{diff.Stack.StackId.ToString()}");
                AddInsertStackMediaToCommands(diff, updates);
                return updates;
            default:
                throw new CatExceptionInternalFailure($"unknown pending up: {diff.PendingOp}");
        }
    }

    public static void UpdateMediaStacks(List<MediaStackDiff> diffs)
    {
        Guid crid = Guid.NewGuid();
        ISql sql = LocalServiceClient.GetConnection();

        List<string> commands = new();

        foreach (MediaStackDiff diff in diffs)
        {
            AddUpdatesForDiff(diff, commands);
        }
        sql.BeginTransaction();

        try
        {
            // build a list of tags to insert as well
            List<string> updateTags = new();

            // take advantage of the enumeration we are going to do across all the
            // items. when we are asked to build the string for each line, we can
            // also build the list of tags we have to insert for these items
            Media.ExecutePartedCommands(
                sql,
                string.Empty,
                commands,
                (command) => command,
                1000,
                " ",
                s_aliases);

            sql.Commit();
        }
        catch (Exception)
        {
            sql.Rollback();
            throw;
        }
        finally
        {
            sql.Close();
        }
    }
}
