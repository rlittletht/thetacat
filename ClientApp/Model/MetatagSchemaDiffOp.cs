using System;

namespace Thetacat.Model;

public class MetatagSchemaDiffOp
{
    public enum ActionType
    {
        Insert, Update, Delete
    }

    // there is always an ID
    public Guid ID { get; init; }

    public ActionType Action { get; init; }
    public Metatag Metatag => m_metatag ?? throw new Exception("no metatag set");
    public bool IsNameChanged => (m_updatedValues & UpdatedValues.Name) != 0;
    public bool IsDescriptionChanged => (m_updatedValues & UpdatedValues.Description) != 0;
    public bool IsParentChanged => (m_updatedValues & UpdatedValues.ParentID) != 0;

    [Flags]
    enum UpdatedValues
    {
        ParentID = 0x01,
        Name = 0x02,
        Description = 0x04
    }

    private Metatag? m_metatag;
    private UpdatedValues m_updatedValues = 0;

    public static MetatagSchemaDiffOp CreateDelete(Guid id)
    {
        return new MetatagSchemaDiffOp()
               {
                   Action = ActionType.Delete,
                   ID = id
               };
    }

    public static MetatagSchemaDiffOp CreateInsert(Metatag metatag)
    {
        return new MetatagSchemaDiffOp()
               {
                   Action = ActionType.Insert,
                   ID = metatag.ID,
                   m_metatag =  metatag
               };
    }

    public static MetatagSchemaDiffOp CreateUpdate(Metatag original, Metatag updated)
    {
        MetatagSchemaDiffOp op = 
            new MetatagSchemaDiffOp()
               {
                   Action = ActionType.Update,
                   ID = updated.ID,
                   m_metatag = updated
               };

        if (original.Name != updated.Name)
            op.m_updatedValues |= UpdatedValues.Name;
        if (original.Parent != updated.Parent)
            op.m_updatedValues |= UpdatedValues.ParentID;
        if (original.Description != updated.Description)
            op.m_updatedValues &= UpdatedValues.Description;

        return op;
    }

}
