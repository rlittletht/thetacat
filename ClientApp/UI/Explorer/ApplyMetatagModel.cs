using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Thetacat.Metatags;
using Thetacat.Model.Metatags;

namespace Thetacat.UI.Explorer;

public class ApplyMetatagModel
{
    public IMetatagTreeItem? RootAvailable
    {
        get => m_rootAvailable;
        set => m_rootAvailable = value;
    }

    public IMetatagTreeItem? RootApplied
    {
        get => m_rootApplied;
        set => m_rootApplied = value;
    }

    private IMetatagTreeItem? m_rootAvailable;
    private IMetatagTreeItem? m_rootApplied;

    public int SelectedItemsVectorClock { get; set; } = 0;
}
