using System.Collections.ObjectModel;

namespace Thetacat.Metatags;

public interface IMetatagTreeItem
{
    public ObservableCollection<IMetatagTreeItem> Children { get; }
    public string Description { get; }
    public string Name { get; }
    public string ID { get; }
}
