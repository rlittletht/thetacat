using Thetacat.Model.ImageCaching;

namespace Thetacat.UI;

public class CacheInfoModel
{
    public ImageCache ExplorerCache { get; init; }
    public ImageCache FullDetailCache { get; init; }

    public CacheInfoModel()
    {
        ExplorerCache = App.State.PreviewImageCache;
        FullDetailCache = App.State.ImageCache;
    }
}
