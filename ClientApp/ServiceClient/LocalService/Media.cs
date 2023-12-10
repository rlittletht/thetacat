using System;
using System.Collections.Generic;
using System.Text;
using TCore;
using Thetacat.Import;
using Thetacat.Model;

namespace Thetacat.ServiceClient.LocalService;

public class Media
{
    private static Dictionary<string, string> s_aliases =
        new()
        {
            { "tcat_media", "MT" },
        };

    private static readonly string s_queryInsertMedia = @"
        INSERT INTO tcat_media
            (id, virtualPath, mimeType, state)
        VALUES ";

    private static readonly string s_queryInsertMediaTag = @"
        INSERT INTO tcat_mediatags
            (id, metatag, value)
        VALUES ";

    private static void ExecutePartedCommands<T>(string commandBase, IEnumerable<T> items, Func<T, string> buildLine, Dictionary<string, string>? aliases)
    {
        StringBuilder sb = new StringBuilder();
        int current = 0;

        sb.Clear();
        sb.Append(commandBase);

        foreach (T item in items)
        {
            if (current == 1000)
            {
                LocalServiceClient.Sql.ExecuteNonQuery(new SqlCommandTextInit(sb.ToString(), aliases));
                current = 0;
                sb.Clear();
                sb.Append(commandBase);
            }

            if (current > 0)
                sb.Append(",");

            sb.Append(buildLine(item));

            current++;
        }
        if (current > 0)
            LocalServiceClient.Sql.ExecuteNonQuery(new SqlCommandTextInit(sb.ToString(), aliases));
    }

    public static void InsertNewMediaItems(IEnumerable<MediaItem> items)
    {
        Guid crid = Guid.NewGuid();
        LocalServiceClient.EnsureConnected();

        LocalServiceClient.Sql.BeginTransaction();

        try
        {
            // build a list of tags to insert as well
            List<ServiceMediaTag> tagsToInsert = new();

            // take advantage of the enumeration we are going to do across all the
            // items. when we are asked to build the string for each line, we can
            // also build the list of tags we have to insert for these items
            ExecutePartedCommands(
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
                        $"('{Sql.Sqlify(item.ID.ToString())}', '{Sql.Sqlify(item.VirtualPath)}', '{Sql.Sqlify(item.MimeType)}', '{MediaItem.StringFromState(item.State)}') ";
                },
                s_aliases);

            ExecutePartedCommands(
                s_queryInsertMediaTag,
                tagsToInsert,
                item =>
                    $"('{Sql.Sqlify(item.MediaId.ToString())}', '{item.Id}', '{Sql.Sqlify(item.Value)}') ",
                s_aliases);


        }
        catch (Exception)
        {
            LocalServiceClient.Sql.Rollback();
            throw;
        }

        LocalServiceClient.Sql.Commit();
    }

}
