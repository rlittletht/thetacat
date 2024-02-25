using System.Security.RightsManagement;
using Thetacat.Types;

namespace Thetacat.Import.UI;

public class VirtualRootNameItem: IBackingTreeItemData
{
    public string Name { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool? Checked { get; set; } = false;
    public string FullName { get; set; }

    public VirtualRootNameItem(string name, string fullname)
    {
        Name = name;
        FullName = fullname;
    }

    public VirtualRootNameItem(string fullName)
    {
        Name = fullName;
        FullName = fullName;
    }

    public override string ToString() => FullName;
}
