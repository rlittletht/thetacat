using System;
using System.Collections.Generic;
using System.Text;
using TCore;
using Thetacat.Logging;
using Thetacat.Model;
using Thetacat.Types;

namespace Thetacat.ServiceClient.LocalService;

public class Media
{
    private static Dictionary<string, string> s_aliases =
        new()
        {
            { "tcat_media", "MT" },
            { "tcat_mediatags", "TMT" }
        };

    private static readonly string s_queryInsertMedia = @"
        INSERT INTO tcat_media
            (id, virtualPath, mimeType, state, md5)
        VALUES ";

    private static readonly string s_queryInsertMediaTag = @"
        INSERT INTO tcat_mediatags
            (id, metatag, value)
        VALUES ";

    private static readonly string s_deleteAllMediaAndMediaTags = @"
        DELETE FROM tcat_mediatags WHERE EXISTS (SELECT * FROM $$#tcat_media$$ WHERE tcat_mediatags.id=$$tcat_media$$.id)
        DELETE FROM tcat_media";

    public static void ExecutePartedCommands<T>(Sql sql, string commandBase, IEnumerable<T> items, Func<T, string> buildLine, int partLimit, string joinString, Dictionary<string, string>? aliases)
    {
        StringBuilder sb = new StringBuilder();
        int current = 0;

        sb.Clear();
        sb.Append(commandBase);

        foreach (T item in items)
        {
            if (current == partLimit)
            {
                string command = sb.ToString();

                if (!string.IsNullOrWhiteSpace(command))
                {
                    LocalServiceClient.LogService?.Invoke(EventType.Verbose, command);
                    sql.ExecuteNonQuery(new SqlCommandTextInit(sb.ToString(), aliases));
                    current = 0;
                }

                sb.Clear();
                sb.Append(commandBase);
            }

            if (current > 0)
                sb.Append(joinString);

            sb.Append(buildLine(item));

            current++;
        }

        if (current > 0)
        {
            string sCmd = sb.ToString();

            if (!string.IsNullOrWhiteSpace(sCmd))
            {
                LocalServiceClient.LogService?.Invoke(EventType.Verbose, sCmd);
                sql.ExecuteNonQuery(new SqlCommandTextInit(sCmd, aliases));
            }
        }
    }

    /*----------------------------------------------------------------------------
        %%Function: InsertNewMediaItems
        %%Qualified: Thetacat.ServiceClient.LocalService.Media.InsertNewMediaItems

        Currently unused. This takes advantage of the multiple values insert
        syntax
    ----------------------------------------------------------------------------*/
    public static void InsertNewMediaItems(IEnumerable<MediaItem> items)
    {
        Guid crid = Guid.NewGuid();
        Sql sql = LocalServiceClient.GetConnection();

        sql.BeginTransaction();

        try
        {
            // build a list of tags to insert as well
            List<ServiceMediaTag> tagsToInsert = new();

            // take advantage of the enumeration we are going to do across all the
            // items. when we are asked to build the string for each line, we can
            // also build the list of tags we have to insert for these items
            ExecutePartedCommands(
                sql,
                s_queryInsertMedia,
                items,
                item =>
                {
                    foreach (Model.MediaTag tag in item.Tags.Values)
                    {
                        tagsToInsert.Add(
                            new ServiceMediaTag()
                            {
                                Id = tag.Metatag.ID,
                                MediaId = item.ID,
                                Value = tag.Value
                            });
                    }

                    return
                        $"('{Sql.Sqlify(item.ID.ToString())}', '{Sql.Sqlify(item.VirtualPath)}', '{Sql.Sqlify(item.MimeType)}', '{MediaItem.StringFromState(item.State)}', '{Sql.Sqlify(item.MD5)}') ";
                },
                1000,
                ", ",
                s_aliases);

            ExecutePartedCommands(
                sql,
                s_queryInsertMediaTag,
                tagsToInsert,
                item =>
                    $"('{Sql.Sqlify(item.MediaId.ToString())}', '{item.Id}', '{Sql.Sqlify(item.Value)}') ",
                1000,
                ", ",
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

    static readonly string s_queryFullCatalog = @"
            SELECT $$tcat_media$$.id, $$tcat_media$$.virtualPath, $$tcat_media$$.mimeType, $$tcat_media$$.state, $$tcat_media$$.md5
            FROM $$#tcat_media$$";

    public static List<ServiceMediaItem> ReadFullCatalogMedia()
    {
        return LocalServiceClient.DoGenericQueryWithAliases<List<ServiceMediaItem>>(
            s_queryFullCatalog,
            s_aliases,
            (SqlReader reader, Guid correlationId, ref List<ServiceMediaItem> building) =>
            {
                Guid mediaId = reader.Reader.GetGuid(0);
                    building.Add(
                        new ServiceMediaItem()
                        {
                            Id = mediaId,
                            VirtualPath = reader.Reader.GetString(1),
                            MimeType = reader.Reader.GetString(2),
                            State = reader.Reader.GetString(3),
                            MD5 = reader.Reader.GetString(4)
                        });
            });
    }

    static readonly string s_queryFullMediaTags = @"
            SELECT $$tcat_mediatags$$.id, $$tcat_mediatags$$.metatag, $$tcat_mediatags$$.value
            FROM $$#tcat_mediatags$$";

    public static List<ServiceMediaTag> ReadFullCatalogMediaTags()
    {
        HashSet<Guid> mediaAdded = new();

        return LocalServiceClient.DoGenericQueryWithAliases<List<ServiceMediaTag>>(
            s_queryFullMediaTags,
            s_aliases,
            (SqlReader reader, Guid correlationId, ref List<ServiceMediaTag> building) =>
            {
                Guid mediaId = reader.Reader.GetGuid(0);
                building.Add(
                    new ServiceMediaTag()
                    {
                        Id = reader.Reader.GetGuid(1),
                        MediaId = mediaId,
                        Value = reader.Reader.IsDBNull(2) ? null : reader.Reader.GetString(2)
                    });
            });
    }

    static readonly string s_queryFullCatalogWithTags = @"
            SELECT $$tcat_media$$.id, $$tcat_media$$.virtualPath, $$tcat_media$$.mimeType, $$tcat_media$$.state, $$tcat_mediatags$$.metatag, $$tcat_mediatags$$.value, $$tcat_media$$.md5
            FROM $$#tcat_media$$
            FULL OUTER JOIN $$#tcat_mediatags$$ ON $$tcat_mediatags$$.id = $$tcat_media$$.id";

    public static ServiceCatalog ReadFullCatalog_OldWithJoin()
    {
        HashSet<Guid> mediaAdded = new();

        return LocalServiceClient.DoGenericQueryWithAliases<ServiceCatalog>(
            s_queryFullCatalogWithTags,
            s_aliases,
            (SqlReader reader, Guid correlationId, ref ServiceCatalog building) =>
            {
                if (building.MediaItems == null || building.MediaTags == null)
                {
                    building.MediaItems = new List<ServiceMediaItem>();
                    building.MediaTags = new List<ServiceMediaTag>();
                }

                Guid mediaId = reader.Reader.GetGuid(0);
                if (!mediaAdded.Contains(mediaId))
                {
                    building.MediaItems.Add(
                        new ServiceMediaItem()
                        {
                            Id = mediaId,
                            VirtualPath = reader.Reader.GetString(1),
                            MimeType = reader.Reader.GetString(2),
                            State = reader.Reader.GetString(3),
                            MD5 = reader.Reader.GetString(6)
                        });
                    mediaAdded.Add(mediaId);
                }

                if (!reader.Reader.IsDBNull(4))
                {
                    building.MediaTags.Add(
                        new ServiceMediaTag()
                        {
                            MediaId = mediaId,
                            Id = reader.Reader.GetGuid(4),
                            Value = reader.Reader.IsDBNull(5) ? null : reader.Reader.GetString(5)
                        });
                }
            });
    }

    static string BuildInsertItemSql(MediaItemDiff diffOp)
    {
        if (diffOp.ItemData == null)
            throw new CatExceptionInternalFailure("itemdata not set for insert");

        if (diffOp.DiffOp != MediaItemDiff.Op.Insert)
            throw new CatExceptionInternalFailure($"insert mediaitem not Op.Insert: {diffOp.DiffOp}");
        
        string id = diffOp.ID.ToString();
        string virtualPath = Sql.Sqlify(diffOp.ItemData.VirtualPath);
        string mimeType = Sql.Sqlify(diffOp.ItemData.MimeType);
        string state = MediaItem.StringFromState(diffOp.ItemData.State);
        string md5 = Sql.Sqlify(diffOp.ItemData.MD5);

        return "INSERT INTO tcat_media (id, virtualPath, mimeType, state, md5) "
            + $"VALUES ('{id}', '{virtualPath}', '{mimeType}', '{state}', '{md5}') ";
    }

    static string BuildDeleteSql(MediaItemDiff diffOp)
    {
        return $"DELETE FROM tcat_media WHERE ID = '{diffOp.ID}'";
    }

    static string BuildUpdateItemSql(MediaItemDiff diffOp)
    {
        switch (diffOp.DiffOp)
        {
            case MediaItemDiff.Op.Insert:
                return BuildInsertItemSql(diffOp);
            case MediaItemDiff.Op.Update:
                return BuildChangeItemSql(diffOp);
            case MediaItemDiff.Op.Delete:
                return BuildDeleteSql(diffOp);
            default:
                throw new CatExceptionInternalFailure($"unknown diffOp op: {diffOp.DiffOp}");
        }
    }

    static string BuildChangeItemSql(MediaItemDiff diffOp)
    {
        if (diffOp.ItemData == null)
            throw new CatExceptionInternalFailure("itemdata not set for update");

        List<string> sets = new();

        if (diffOp.IsMimeTypeChanged)
            sets.Add($"MimeType='{Sql.Sqlify(diffOp.ItemData.MimeType)}'");
        if (diffOp.IsMD5Changed)
            sets.Add($"MD5='{Sql.Sqlify(diffOp.ItemData.MD5)}'");
        if (diffOp.IsPathChanged)
            sets.Add($"VirtualPath={MediaItem.StringFromState(diffOp.ItemData.State)}");

        if (sets.Count == 0)
            return "";

        string setsSql = string.Join(", ", sets.ToArray());
        return $"UPDATE tcat_media SET {setsSql} WHERE ID='{diffOp.ID.ToString()}'";
    }

    static string BuildMediaTagDelete(Guid mediaId, Guid metatagId)
    {
        return $"DELETE FROM tcat_mediatags WHERE id='{mediaId.ToString()}' AND metatag='{metatagId.ToString()}' ";
    }

    static string BuildMediaTagInsert(Guid mediaId, MediaTag mediaTag)
    {
        string? value = mediaTag.Value == null ? null : Sql.Sqlify(mediaTag.Value);

        return $"INSERT INTO tcat_mediatags (id, metatag, value) VALUES ('{mediaId.ToString()}', '{mediaTag.Metatag.ID.ToString()}', {Sql.Nullable(value)}) ";
    }

    static string BuildMediaTagUpdate(Guid mediaId, MediaTag mediaTag)
    {
        string? value = mediaTag.Value == null ? null : Sql.Sqlify(mediaTag.Value);

        return $"UPDATE tcat_mediatags SET value = {Sql.Nullable(value)} WHERE id='{mediaId.ToString()}' AND metatag='{mediaTag.Metatag.ID.ToString()}' ";
    }

    static List<string> BuildUpdateItemTagsSql(MediaItemDiff diffOp)
    {
        List<string> sets = new();

        switch (diffOp.DiffOp)
        {
            case MediaItemDiff.Op.Insert:
            {
                if (diffOp.ItemData == null)
                    throw new CatExceptionInternalFailure("no itemdata for insert");
                // all the tags get inserted
                foreach (KeyValuePair<Guid, MediaTag> tag in diffOp.ItemData.Tags)
                {
                    sets.Add(BuildMediaTagInsert(diffOp.ID, tag.Value));
                }

                return sets;
            }
            case MediaItemDiff.Op.Delete:
                // all the media tags associated with this media ID gets deleted
                sets.Add($"DELETE FROM tcat_mediatags WHERE id='{diffOp.ID.ToString()}' ");
                return sets;
            case MediaItemDiff.Op.Update:
                if (!diffOp.IsTagsChanged
                    || diffOp.TagDiffs == null
                    || diffOp.TagDiffs.Count == 0)
                {
                    return sets;
                }

                // for existing mediaitmes that are being updated, individual tags can be
                // added, updated, or deleted...
                foreach (MediaTagDiff tagDiff in diffOp.TagDiffs)
                {
                    switch (tagDiff.DiffOp)
                    {
                        case MediaTagDiff.Op.Delete:
                            sets.Add(BuildMediaTagDelete(diffOp.ID, tagDiff.ID));
                            break;
                        case MediaTagDiff.Op.Insert:
                            sets.Add(BuildMediaTagInsert(diffOp.ID, tagDiff.MediaTag ?? throw new CatExceptionInternalFailure("mediatag not set for insert")));
                            break;
                        case MediaTagDiff.Op.Update:
                            sets.Add(BuildMediaTagUpdate(diffOp.ID, tagDiff.MediaTag ?? throw new CatExceptionInternalFailure("mediatag not set for insert")));
                            break;
                        default:
                            throw new CatExceptionInternalFailure($"unknown diffop: {tagDiff.DiffOp}");
                    }
                }

                return sets;
            default:
                throw new CatExceptionInternalFailure($"unknown MediaItemDiff.Op {diffOp.DiffOp}");
        }
    }

    public static void UpdateMediaItems(IEnumerable<MediaItemDiff> diffs)
    {
        Guid crid = Guid.NewGuid();
        Sql sql = LocalServiceClient.GetConnection();

        sql.BeginTransaction();

        try
        {
            // build a list of tags to insert as well
            List<string> updateTags = new();

            // take advantage of the enumeration we are going to do across all the
            // items. when we are asked to build the string for each line, we can
            // also build the list of tags we have to insert for these items
            ExecutePartedCommands(
                sql,
                string.Empty,
                diffs,
                diff =>
                {
                    updateTags.AddRange(BuildUpdateItemTagsSql(diff));
                    return BuildUpdateItemSql(diff);
                },
                1000,
                " ",
                s_aliases);

            ExecutePartedCommands(
                sql,
                string.Empty,
                updateTags,
                updateTag => updateTag,
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

    public static void DeleteAllMediaAndMediaTags()
    {
        LocalServiceClient.DoGenericCommandWithAliases(s_deleteAllMediaAndMediaTags, s_aliases, null);
    }
}
