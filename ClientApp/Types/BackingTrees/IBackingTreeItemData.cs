namespace Thetacat.Types;

public interface IBackingTreeItemData
{
    public string Name { get; }
    public string Description { get; }
    public bool? Checked { get; set; }
}
