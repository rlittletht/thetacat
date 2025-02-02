﻿using System;
using Thetacat.Model.Mediatags;

namespace Thetacat.Model;

public class MediaTagDiff
{
    public enum Op
    {
        Insert,
        Update,
        Delete
    }

    public Op DiffOp { get; set; }
    public MediaTag? MediaTag { get; set; }
    public Guid ID { get; set; }

    public MediaTagDiff(Guid id)
    {
        ID = id;
    }

    public static MediaTagDiff CreateDelete(Guid id)
    {
        return
            new MediaTagDiff(id)
            {
                DiffOp = Op.Delete
            };
    }

    public static MediaTagDiff CreateInsert(MediaTag tag)
    {
        return
            new MediaTagDiff(tag.Metatag.ID)
            {
                DiffOp = Op.Insert,
                MediaTag = tag
            };
    }

    /*----------------------------------------------------------------------------
        %%Function: CreateResurrect
        %%Qualified: Thetacat.Model.MediaTagDiff.CreateResurrect

        For now, this is just an update...we might create an Op.Resurrect if update
        isn't enough
    ----------------------------------------------------------------------------*/
    public static MediaTagDiff CreateResurrect(MediaTag tag)
    {
        return
            new MediaTagDiff(tag.Metatag.ID)
            {
                DiffOp = Op.Update,
                MediaTag = tag
            };
    }

    public static MediaTagDiff CreateUpdate(MediaTag tag)
    {
        return
            new MediaTagDiff(tag.Metatag.ID)
            {
                DiffOp = Op.Update,
                MediaTag = tag
            };
    }
}
