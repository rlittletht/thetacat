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
            WHERE $$tcat_mediatags$$.catalog_id=@CatalogID AND $$tcat_mediatags$$.clock >= @MinClock";

    private static readonly string s_removeDeletedTagsAndResetTagClock = @"
        BEGIN TRANSACTION
            UPDATE tcat_vector_clocks SET value = value + 1 WHERE name='mediatag-reset-clock' AND catalog_id=@Catalog
            DELETE FROM tcat_mediatags WHERE catalog_id=@Catalog AND deleted = 1
        COMMIT TRANSACTION";

    private static readonly string s_getPendingClockCount = @"
        SELECT count(*) FROM tcat_mediatags WHERE catalog_id=@CatalogID AND clock=0";

    private static readonly string s_disableClockIndex = @"
        ALTER INDEX idx_tcat_mediatags_new_clock ON tcat_mediatags DISABLE";

    private static readonly string s_rebuildClockIndex = @"
        ALTER INDEX idx_tcat_mediatags_new_clock ON tcat_mediatags REBUILD";

    public static int GetMediatagsPendingClockCount(Guid catalogID)
    {
        ISql? sql = null;

        try
        {
            sql = LocalServiceClient.GetConnection();

            return sql.NExecuteScalar(
                new SqlCommandTextInit(s_getPendingClockCount),
                cmd => cmd.AddParameterWithValue("@CatalogID", catalogID));
        }
        finally
        {
            sql?.Close();
        }
    }

    public static void DisableClockIndex()
    {
        ISql? sql = null;

        try
        {
            sql = LocalServiceClient.GetConnection();

            sql.ExecuteNonQuery(new SqlCommandTextInit(s_disableClockIndex));
        }
        finally
        {
            sql?.Close();
        }
    }

    public static void RebuildClockIndex()
    {
        ISql? sql = null;

        try
        {
            sql = LocalServiceClient.GetConnection();

            sql.ExecuteNonQuery(
                new SqlCommandTextInit(s_rebuildClockIndex),
                cmd => cmd.CommandTimeout = 300);
        }
        finally
        {
            sql?.Close();
        }
    }

    public static void RemoveDeletedMediatagsAndResetTagClock(Guid catalogId)
    {
        ISql? sql = null;

        try
        {
            sql = LocalServiceClient.GetConnection();

            sql.ExecuteNonQuery(
                new SqlCommandTextInit(s_removeDeletedTagsAndResetTagClock),
                cmd => cmd.AddParameterWithValue("@Catalog", catalogId));
        }
        catch
        {
        }
        finally
        {
            sql?.Close();
        }
    }
    private static readonly string s_setPendingTagVectorClocks = @"
        DECLARE @version AS int
        DECLARE @count AS int
        
        BEGIN TRANSACTION

        BEGIN TRY
	        SET @version = (SELECT [value] FROM tcat_vector_clocks where catalog_id = @CatalogID AND name = 'mediatag-clock');

	        UPDATE tcat_mediatags SET clock = @version + 1 WHERE catalog_id = @CatalogID AND clock = 0

            set @count = @@ROWCOUNT
        
            IF @count > 0
                BEGIN
	                IF EXISTS(
		                SELECT 1
		                FROM tcat_vector_clocks WITH (UPDLOCK,HOLDLOCK)
		                WHERE catalog_id = @CatalogID AND name = 'mediatag-clock' AND [value] = @version 
		                )
		                BEGIN
			                UPDATE tcat_vector_clocks SET [value] = @version + 1 WHERE catalog_id = @CatalogID AND name = 'mediatag-clock' AND [value] = @version
		                END
	                ELSE
		                BEGIN
			                THROW 51000, 'Coherency failure', 1
		                END
                END
	        COMMIT TRANSACTION
        END TRY
        BEGIN CATCH
	        ROLLBACK TRANSACTION
	        RAISERROR('Coherency Failure', 18, 1)
        END CATCH";

    private static readonly string s_setPendingTagVectorClocksBatched = @"
        DECLARE @version AS int
        DECLARE @count AS int
        
        BEGIN TRANSACTION

        BEGIN TRY
	        SET @version = (SELECT [value] FROM tcat_vector_clocks where catalog_id = @CatalogID AND name = 'mediatag-clock');

	        UPDATE TOP(@BatchSize) tcat_mediatags SET clock = @version + 1 WHERE catalog_id = @CatalogID AND clock = 0

            set @count = @@ROWCOUNT
        
            IF @count > 0
                BEGIN
	                IF EXISTS(
		                SELECT 1
		                FROM tcat_vector_clocks WITH (UPDLOCK,HOLDLOCK)
		                WHERE catalog_id = @CatalogID AND name = 'mediatag-clock' AND [value] = @version 
		                )
		                BEGIN
			                UPDATE tcat_vector_clocks SET [value] = @version + 1 WHERE catalog_id = @CatalogID AND name = 'mediatag-clock' AND [value] = @version
		                END
	                ELSE
		                BEGIN
			                THROW 51000, 'Coherency failure', 1
		                END
                END
	        COMMIT TRANSACTION
            SELECT @count
        END TRY
        BEGIN CATCH
	        ROLLBACK TRANSACTION
	        RAISERROR('Coherency Failure', 18, 1)
        END CATCH";

    /*----------------------------------------------------------------------------
        %%Function: UpdateMediatagsWithNoClockAndincrementVectorClock
        %%Qualified: Thetacat.ServiceClient.LocalService.Mediatags.UpdateMediatagsWithNoClockAndincrementVectorClock

        Complicated statement to update all of the mediatags that have a clock
        of 0.  this will set the clock to the next clock according to the
        "mediatag-clock" in the database. BUT, if the mediatag-clock changes
        during the update, then this will rollback the transaction and raise an
        error.

        IF the clock doesn't change AND at least one mediatag was updated, then
        the global mediatag-clock will get incremented
    ----------------------------------------------------------------------------*/
    public static void UpdateMediatagsWithNoClockAndincrementVectorClock(Guid catalogId)
    {
        ISql? sql = null;

        try
        {
            sql = LocalServiceClient.GetConnection();
            
            sql.ExecuteNonQuery(
                new SqlCommandTextInit(s_setPendingTagVectorClocks),
                cmd =>
                {
                    cmd.CommandTimeout = 300; // 5 minute timeout
                    cmd.AddParameterWithValue("@CatalogID", catalogId);
                });
        }
        // we want exceptions to get thrown
        finally
        {
            sql?.Close();
        }
    }

    /*----------------------------------------------------------------------------
        %%Function: UpdateMediatagsWithNoClockAndincrementVectorClockBatched
        %%Qualified: Thetacat.ServiceClient.LocalService.Mediatags.UpdateMediatagsWithNoClockAndincrementVectorClockBatched

        Same as UpdateMediatagsWithNoClockAndincrementVectorClock but takes a
        batch size. Returns the count of rows affected. If 0, we're done
    ----------------------------------------------------------------------------*/
    public static int UpdateMediatagsWithNoClockAndincrementVectorClockBatched(Guid catalogId, int batchSize)
    {
        ISql? sql = null;

        try
        {
            sql = LocalServiceClient.GetConnection();

            return sql.NExecuteScalar(
                new SqlCommandTextInit(s_setPendingTagVectorClocksBatched),
                cmd =>
                {
                    cmd.CommandTimeout = 300; // 5 minute timeout
                    cmd.AddParameterWithValue("@CatalogID", catalogId);
                    cmd.AddParameterWithValue("@BatchSize", batchSize);
                });
        }
        // we want exceptions to get thrown
        finally
        {
            sql?.Close();
        }
    }

    /*----------------------------------------------------------------------------
        %%Function: ReadFullCatalogMediaTags
        %%Qualified: Thetacat.ServiceClient.LocalService.Mediatags.ReadFullCatalogMediaTags
    ----------------------------------------------------------------------------*/
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

    /*----------------------------------------------------------------------------
        %%Function: ReadMediaTagsForClock
        %%Qualified: Thetacat.ServiceClient.LocalService.Mediatags.ReadMediaTagsForClock
    ----------------------------------------------------------------------------*/
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
//        return $"DELETE FROM tcat_mediatags WHERE id='{mediaId}' AND metatag='{metatagId}' AND catalog_id='{cataogID}' ";
        return $"UPDATE tcat_mediatags SET Deleted=1, Clock = 0 WHERE id='{mediaId}' AND metatag='{metatagId}' AND catalog_id='{catalogID}' ";
    }

    public static string BuildMediaTagInsert(Guid catalogID, Guid mediaId, MediaTag mediaTag)
    {
        string? value = mediaTag.Value == null ? null : SqlText.Sqlify(mediaTag.Value);

        return
            $"INSERT INTO tcat_mediatags (catalog_id, id, metatag, value, deleted, clock) VALUES ('{catalogID}', '{mediaId}', '{mediaTag.Metatag.ID}', {SqlText.Nullable(value)}, 0, 0) ";
    }

    public static string BuildMediaTagUpdate(Guid catalogID, Guid mediaId, MediaTag mediaTag)
    {
        string? value = mediaTag.Value == null ? null : SqlText.Sqlify(mediaTag.Value);

        return
            $"UPDATE tcat_mediatags SET value = {SqlText.Nullable(value)}, Clock = 0, Deleted = {(mediaTag.Deleted ? 1 : 0)} WHERE id='{mediaId}' AND metatag='{mediaTag.Metatag.ID}' AND catalog_id='{catalogID}' ";
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
                sets.Add($"UPDATE tcat_mediatags SET Clock = 0, Deleted = 1 WHERE id='{diffOp.ID}' AND catalog_id='{catalogID}' ");
//                sets.Add($"DELETE FROM tcat_mediatags WHERE id='{diffOp.ID}' AND catalog_id='{catalogID}' ");
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
