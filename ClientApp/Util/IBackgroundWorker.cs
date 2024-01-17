namespace Thetacat.Util;

public interface IBackgroundWorker
{
    public string Description { get; set; }
    public bool IsIndeterminate { get; set; }
    public int TenthPercentComplete { get; set; }
}
