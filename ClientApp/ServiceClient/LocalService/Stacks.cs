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
            ON $$tcat_stacks$$.id = $$tcat_stackmedia$$.id
        WHERE
            $$tcat_stacks$$.catalog_id = @CatalogID";

    private static readonly string s_deleteAllStacksWithMedia = @"
        DELETE FROM tcat_stacks WHERE EXISTS (SELECT * FROM $$#tcat_stackmedia$$ INNER JOIN $$#tcat_media$$ ON $$tcat_stackmedia$$.media_id=$$tcat_media$$.id WHERE $$tcat_stackmedia$$.id=tcat_stacks.id) WHERE tcat_stacks.catalog_id=@CatalogID
        DELETE FROM tcat_stackmedia WHERE EXISTS (SELECT * FROM $$#tcat_media$$ WHERE tcat_stackmedia.id=$$tcat_media$$.id) WHERE tcat_stackmedia.catalog_id=@CatalogID";

    public static void DeleteAllStacksAssociatedWithMedia(Guid catalogID)
    {
        LocalServiceClient.DoGenericCommandWithAliases(s_deleteAllStacksWithMedia, s_aliases, cmd => cmd.AddParameterWithValue("@CatalogID", catalogID));
    }

    public static List<ServiceStack> GetAllStacks(Guid catalogID)
    {
        Dictionary<Guid, ServiceStack> mapStack = new Dictionary<Guid, ServiceStack>();

        return LocalServiceClient.DoGenericQueryWithAliases(
            s_queryAllStacks,
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
            },
            s_aliases,
            cmd => cmd.AddParameterWithValue("@CatalogID", catalogID));
    }

    public static void AddInsertStackMediaToCommands(Guid catalogID, MediaStackDiff diff, List<string> updates)
    {
        foreach (MediaStackItem item in diff.Stack.Items)
        {
            updates.Add(
                $"INSERT INTO tcat_stackmedia (catalog_id, id, media_id, orderHint) VALUES ('{catalogID}', '{diff.Stack.StackId}', '{item.MediaId}', {item.StackIndex})");
        }
    }

    public static IEnumerable<string> AddUpdatesForDiff(Guid catalogID, MediaStackDiff diff, List<string> updates)
    {
        switch (diff.PendingOp)
        {
            case MediaStack.Op.Create:
                updates.Add($"INSERT INTO tcat_stacks (catalog_id, id, stackType, description) VALUES ('{catalogID}','{diff.Stack.StackId}', {SqlText.SqlifyQuoted(diff.Stack.Type)}, {SqlText.SqlifyQuoted(diff.Stack.Description)})");
                AddInsertStackMediaToCommands(catalogID, diff, updates);
                return updates;
            case MediaStack.Op.Delete:
                updates.Add($"DELETE FROM tcat_stacks WHERE id='{diff.Stack.StackId}' AND catalog_id='{catalogID}'");
                updates.Add($"DELETE FROM tcat_stackmedia WHERE id='{diff.Stack.StackId}' AND catalog_id='{catalogID}'");
                return updates;
            case MediaStack.Op.Update:
                updates.Add($"DELETE FROM tcat_stacks WHERE id='{diff.Stack.StackId}' AND catalog_id='{catalogID}'");
                updates.Add(
                    $"INSERT INTO tcat_stacks (catalog_id, id, stackType, description) VALUES ('{catalogID}','{diff.Stack.StackId}', {SqlText.SqlifyQuoted(diff.Stack.Type)}, {SqlText.SqlifyQuoted(diff.Stack.Description)})");
                updates.Add($"DELETE FROM tcat_stackmedia WHERE id='{diff.Stack.StackId}' AND catalog_id='{catalogID}'");
                AddInsertStackMediaToCommands(catalogID, diff, updates);
                return updates;
            default:
                throw new CatExceptionInternalFailure($"unknown pending up: {diff.PendingOp}");
        }
    }

    public static void UpdateMediaStacks(Guid catalogID, List<MediaStackDiff> diffs)
    {
        List<string> commands = new();

        foreach (MediaStackDiff diff in diffs)
        {
            AddUpdatesForDiff(catalogID, diff, commands);
        }

        LocalServiceClient.DoGenericPartedCommands(
            string.Empty,
            commands,
            command => command,
            1000,
            " ",
            s_aliases);
    }
}
