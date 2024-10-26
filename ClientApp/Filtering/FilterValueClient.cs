using System;
using System.Collections.Generic;
using System.Windows.Forms.Design;
using TCore.PostfixText;
using Thetacat.Metatags;
using Thetacat.Model;
using Thetacat.Model.Mediatags;
using Thetacat.Types;

namespace Thetacat.Filtering;

public class FilterValueClient: PostfixText.IValueClient
{
    private readonly MediaItem m_mediaItem;

    public FilterValueClient(MediaItem mediaItem)
    {
        m_mediaItem = mediaItem;
    }

    /*----------------------------------------------------------------------------
        %%Function: GetStringFromField
        %%Qualified: Thetacat.Filtering.FilterValueClient.GetStringFromField

        Fields right now are just the metatag ID
    ----------------------------------------------------------------------------*/
    public string GetStringFromField(string field)
    {
        if (!field.StartsWith('{') || !field.EndsWith('}'))
            return string.Empty;

        if (!Guid.TryParse(field, out Guid metatagID))
            throw new CatExceptionInternalFailure($"invalid guid format: {field}");

        if (!m_mediaItem.TryGetMediaTag(metatagID, out MediaTag? mediaTag))
        {
            // get the tree item for this metatag

            IMetatagTreeItem? metatagTreeItem = App.State.MetatagSchema.GetTreeItemIfContainer(metatagID);

            if (metatagTreeItem == null)
            {
                return "$false";
            }

            bool matched = false;

            metatagTreeItem.Preorder(
                null,
                (item, parent, depth) =>
                {
                    if (Guid.TryParse(item.ID, out Guid itemID))
                    {
                        matched |= m_mediaItem.HasMediaTag(itemID);
                    }
                },
                0);

            return matched ? "$true" : "$false";
        }

        if (mediaTag.Value == null)
            return "$true"; // without a value, its just "true"

        return mediaTag.Value;
    }

    public int? GetNumberFromField(string field)
    {
        if (!field.StartsWith('{') || !field.EndsWith('}'))
            return null;

        if (!Guid.TryParse(field, out Guid metatagID))
            throw new CatExceptionInternalFailure($"invalid guid format: {field}");

        if (!m_mediaItem.TryGetMediaTag(metatagID, out MediaTag? mediaTag))
            return null;

        // try to coerce the value to a number
        if (!Int32.TryParse(mediaTag.Value ?? "1", out Int32 val))
            throw new CatExceptionInternalFailure($"couldn't coerce value '{mediaTag.Value}' to int");

        return val;
    }

    public DateTime? GetDateTimeFromField(string field)
    {
        // right now, we have no datetime fields. in the future we could support
        if (!field.StartsWith('{') || !field.EndsWith('}'))
            return null;

        if (!Guid.TryParse(field, out Guid metatagID))
            throw new CatExceptionInternalFailure($"invalid guid format: {field}");

        if (!m_mediaItem.TryGetMediaTag(metatagID, out MediaTag? mediaTag))
            return null;

        if (mediaTag.Value == null)
            return null;

        // try to coerce the value to a number
        if (!DateTime.TryParse(mediaTag.Value, out DateTime date))
            throw new CatExceptionInternalFailure($"couldn't coerce value '{mediaTag.Value}' to DateTime");

        if (mediaTag.Value.EndsWith('Z'))
            return date.ToLocalTime();

        return date;
    }

    public Value.ValueType GetFieldValueType(string field)
    {
        string value = field;

        if (field.StartsWith('{') && field.EndsWith('}'))
        {
            if (!Guid.TryParse(field, out Guid metatagID))
                return Value.ValueType.String;

            if (!m_mediaItem.TryGetMediaTag(metatagID, out MediaTag? mediaTag) || mediaTag.Value == null)
                return Value.ValueType.String;

            value = mediaTag.Value;
        }

        // check if this is datetime
        if (DateTime.TryParse(value, out DateTime date))
            return Value.ValueType.DateTime;

        if (Int32.TryParse(value, out Int32 nValue))
            return Value.ValueType.Number;

        return Value.ValueType.String;
    }
}
