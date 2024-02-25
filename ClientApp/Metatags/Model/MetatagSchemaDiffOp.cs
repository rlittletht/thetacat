using System;
using System.Collections.Generic;

namespace Thetacat.Metatags.Model;

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
    public bool IsStandardChanged => (m_updatedValues & UpdatedValues.Standard) != 0;

    [Flags]
    enum UpdatedValues
    {
        ParentID = 0x01,
        Name = 0x02,
        Description = 0x04,
        Standard = 0x08,
    }

    private Metatag? m_metatag;
    private UpdatedValues m_updatedValues = 0;

    public static MetatagSchemaDiffOp CreateForTest(ActionType action, Metatag metatag, bool updateName, bool updateDescription, bool updateStandard, bool updateParent)
    {
        MetatagSchemaDiffOp op =
            new MetatagSchemaDiffOp()
            {
                Action = action,
                ID = metatag.ID,
                m_metatag = metatag
            };

        if (updateParent)
            op.m_updatedValues |= UpdatedValues.ParentID;
        if (updateName)
            op.m_updatedValues |= UpdatedValues.Name;
        if (updateDescription)
            op.m_updatedValues |= UpdatedValues.Description;
        if (updateStandard)
            op.m_updatedValues |= UpdatedValues.Standard;

        return op;
    }

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
            m_metatag = metatag
        };
    }

    public static MetatagSchemaDiffOp CreateUpdate3WM(Metatag _base, Metatag server, Metatag local)
    {
        MetatagSchemaDiffOp op =
            new MetatagSchemaDiffOp()
            {
                Action = ActionType.Update,
                ID = local.ID,
                m_metatag = local
            };

        op.m_metatag.Parent = server.Parent;

        if (_base.Parent == server.Parent && server.Parent != local.Parent)
        {
            // local parent only wins if base==server
            op.m_metatag.Parent = local.Parent;
            op.m_updatedValues |= UpdatedValues.ParentID;
        }

        // for all others, local wins
        if (_base.Name != server.Name)
            op.m_updatedValues |= UpdatedValues.Name;
        if (_base.Description != server.Description)
            op.m_updatedValues |= UpdatedValues.Description;
        if (_base.Standard != server.Standard)
            op.m_updatedValues |= UpdatedValues.Standard;
        return op;
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
            op.m_updatedValues |= UpdatedValues.Description;
        if (original.Standard != updated.Standard)
            op.m_updatedValues |= UpdatedValues.Standard;
        return op;
    }

    public override string ToString()
    {
        switch (Action)
        {
            case ActionType.Insert:
                return $"INSERT {m_metatag}";
            case ActionType.Delete:
                return $"DELETE {ID}";
            case ActionType.Update:
                List<string> changes = new();

                if (IsNameChanged)
                    changes.Add($"Name=>'{Metatag.Name}'");
                if (IsDescriptionChanged)
                    changes.Add($"Description=>'{Metatag.Description}'");
                if (IsParentChanged)
                    changes.Add($"Parent=>{Metatag.Parent}");
                if (IsStandardChanged)
                    changes.Add($"Standard={Metatag.Standard}");

                return $"UPDATE {Metatag.ID}: {string.Join(",", changes)}";
        }

        return "";
    }
}
