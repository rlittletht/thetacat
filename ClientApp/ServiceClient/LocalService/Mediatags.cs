using System;
using System.Collections.Generic;
using TCore.SqlClient;
using TCore.SqlCore;
using Thetacat.Model;
using Thetacat.Model.Mediatags;
using Thetacat.Types;

namespace Thetacat.ServiceClient.LocalService;

public class Mediatags
{
    private static TableAliases s_aliases =
        new(
            new()
            {
                { "tcat_media", "MT" },
                { "tcat_mediatags", "TMT" },
                { "tcat_stackmedia", "SM" },
                { "tcat_stacks", "ST" }
            });

    static readonly string s_queryFullMediaTags = @"
            SELECT $$tcat_mediatags$$.id, $$tcat_mediatags$$.metatag, $$tcat_mediatags$$.value, $$tcat_mediatags$$.clock, $$tcat_mediatags$$.deleted
            FROM $$#tcat_mediatags$$
            WHERE $$tcat_mediatags$$.catalog_id=@CatalogID";

    private static readonly string s_getMediatagClock = @"
        SELECT value FROM tcat_vector_clocks WHERE catalog_id = @CatalogId AND name = 'mediatag-clock'";

    private static readonly string s_getMediatagResetClock = @"
        SELECT value FROM tcat_vector_clocks WHERE catalog_id = @CatalogId AND name = 'mediatag-reset-clock'";

    static readonly string s_queryMediaTagsClock = @"
            SELECT $$tcat_mediatags$$.id, $$tcat_mediatags$$.metatag, $$tcat_mediatags$$.value, $$tcat_mediatags$$.clock, $$tcat_mediatags$$.deleted
            FROM $$#tcat_mediatags$$
            WHERE $$tcat_mediatags$$.catalog_id=@CatalogID AND $$tcat_mediatags$$.clock > @MinClock";

    public static ServiceMediaTagsWithClocks ReadFullCatalogMediaTags(Guid catalogId)
    {
        string sQuery = $"{s_queryFullMediaTags} {s_getMediatagClock} {s_getMediatagResetClock}";

        HashSet<Guid> mediaAdded = new();
        ISql sql = LocalServiceClient.GetConnection();

        try
        {
            return sql.ExecuteMultiSetDelegatedQuery(
                Guid.NewGuid(),
                sQuery,
                (ISqlReader reader, Guid _, int recordset, ref ServiceMediaTagsWithClocks building) =>
                {
                    if (recordset == 0)
                    {
                        Guid mediaId = reader.GetGuid(0);
                        building.Tags.Add(
                            new ServiceMediaTag()
                            {
                                Id = reader.GetGuid(1),
                                MediaId = mediaId,
                                Value = reader.GetNullableString(2),
                                Clock = reader.GetInt32(3),
                                Deleted = reader.GetBoolean(4)
                            });
                    }
                    else if (recordset == 1)
                    {
                        building.TagClock = reader.GetInt32(0);
                    }
                    else if (recordset == 2)
                    {
                        building.ResetClock = reader.GetInt32(0);
                    }
                    else
                    {
                        throw new CatExceptionServiceDataFailure();
                    }
                },
                s_aliases,
                cmd => cmd.AddParameterWithValue("@CatalogID", catalogId));
        }
        catch
        {
            return new ServiceMediaTagsWithClocks()
                   {
                       TagClock = sql.NExecuteScalar(
                           new SqlCommandTextInit(s_getMediatagClock),
                           cmd => cmd.AddParameterWithValue("@CatalogID", catalogId)),
                       ResetClock = sql.NExecuteScalar(
                           new SqlCommandTextInit(s_getMediatagResetClock),
                           cmd => cmd.AddParameterWithValue("@CatalogID", catalogId))
                   };
        }
    }

    public static ServiceMediaTagsWithClocks ReadMediaTagsForClock(Guid catalogId, int minClock)
    {
        string sQuery = $"{s_queryMediaTagsClock} {s_getMediatagClock} {s_getMediatagResetClock}";

        HashSet<Guid> mediaAdded = new();
        ISql sql = LocalServiceClient.GetConnection();

        try
        {
            return sql.ExecuteMultiSetDelegatedQuery(
                    Guid.NewGuid(),
                    sQuery,
                    (ISqlReader reader, Guid _, int recordset, ref ServiceMediaTagsWithClocks building) =>
                    {
                        if (recordset == 0)
                        {
                            Guid mediaId = reader.GetGuid(0);
                            building.Tags.Add(
                                new ServiceMediaTag()
                                {
                                    Id = reader.GetGuid(1),
                                    MediaId = mediaId,
                                    Value = reader.GetNullableString(2),
                                    Clock = reader.GetInt32(3),
                                    Deleted = reader.GetBoolean(4)
                                });
                        }
                        else if (recordset == 1)
                        {
                            building.TagClock = reader.GetInt32(0);
                        }
                        else if (recordset == 2)
                        {
                            building.ResetClock = reader.GetInt32(0);
                        }
                        else
                        {
                            throw new CatExceptionServiceDataFailure();
                        }
                    },
                    s_aliases,
                    cmd =>
                    {
                        cmd.AddParameterWithValue("@CatalogID", catalogId);
                        cmd.AddParameterWithValue("@MinClock", minClock);
                    });
        }
        catch
        {
            return new ServiceMediaTagsWithClocks()
                   {
                       TagClock = sql.NExecuteScalar(
                           new SqlCommandTextInit(s_getMediatagClock),
                           cmd => cmd.AddParameterWithValue("@CatalogID", catalogId)),
                       ResetClock = sql.NExecuteScalar(
                           new SqlCommandTextInit(s_getMediatagResetClock),
                           cmd => cmd.AddParameterWithValue("@CatalogID", catalogId))
                   };
        }
    }

    public static string BuildMediaTagDelete(Guid catalogID, Guid mediaId, Guid metatagId)
    {
        return $"DELETE FROM tcat_mediatags WHERE id='{mediaId}' AND metatag='{metatagId}' AND catalog_id='{catalogID}' ";
    }

    public static string BuildMediaTagInsert(Guid catalogID, Guid mediaId, MediaTag mediaTag)
    {
        string? value = mediaTag.Value == null ? null : SqlText.Sqlify(mediaTag.Value);

        return
            $"INSERT INTO tcat_mediatags (catalog_id, id, metatag, value) VALUES ('{catalogID}', '{mediaId}', '{mediaTag.Metatag.ID}', {SqlText.Nullable(value)}) ";
    }

    public static string BuildMediaTagUpdate(Guid catalogID, Guid mediaId, MediaTag mediaTag)
    {
        string? value = mediaTag.Value == null ? null : SqlText.Sqlify(mediaTag.Value);

        return
            $"UPDATE tcat_mediatags SET value = {SqlText.Nullable(value)} WHERE id='{mediaId}' AND metatag='{mediaTag.Metatag.ID}' AND catalog_id='{catalogID}' ";
    }

    public static List<string> BuildUpdateItemTagsSql(Guid catalogID, MediaItemDiff diffOp)
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
                    sets.Add(BuildMediaTagInsert(catalogID, diffOp.ID, tag.Value));
                }

                return sets;
            }
            case MediaItemDiff.Op.Delete:
                // all the media tags associated with this media ID gets deleted
                sets.Add($"DELETE FROM tcat_mediatags WHERE id='{diffOp.ID}' AND catalog_id='{catalogID}' ");
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
                            sets.Add(BuildMediaTagDelete(catalogID, diffOp.ID, tagDiff.ID));
                            break;
                        case MediaTagDiff.Op.Insert:
                            sets.Add(
                                BuildMediaTagInsert(
                                    catalogID,
                                    diffOp.ID,
                                    tagDiff.MediaTag ?? throw new CatExceptionInternalFailure("mediatag not set for insert")));
                            break;
                        case MediaTagDiff.Op.Update:
                            sets.Add(
                                BuildMediaTagUpdate(
                                    catalogID,
                                    diffOp.ID,
                                    tagDiff.MediaTag ?? throw new CatExceptionInternalFailure("mediatag not set for insert")));
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
}
