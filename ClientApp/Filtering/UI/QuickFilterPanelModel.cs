using Thetacat.Metatags;

namespace Thetacat.Explorer.UI;

public class QuickFilterPanelModel
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
}
