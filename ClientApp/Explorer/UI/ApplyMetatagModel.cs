using Thetacat.Import.UI.Commands;
using Thetacat.Metatags;

namespace Thetacat.Explorer.UI;

public class ApplyMetatagModel
{
    public SetMediaTagValueCommand? SetMediaTagValueCommand { get; set; }

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
