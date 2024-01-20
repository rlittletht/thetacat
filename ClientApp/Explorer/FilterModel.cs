using Thetacat.Metatags;

namespace Thetacat.Explorer;

public class FilterModel
{
    public IMetatagTreeItem? RootAvailable
    {
        get => m_rootAvailable;
        set => m_rootAvailable = value;
    }

    private IMetatagTreeItem? m_rootAvailable;
}
