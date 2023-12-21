using System.Windows;
using Thetacat.Model;
using Thetacat.Model.Metatags;
using Thetacat.ServiceClient;

namespace Thetacat.Types;

public class AppState : IAppState
{
    public delegate void CloseAsyncLogDelegate(bool skipClose);

    private readonly CloseAsyncLogDelegate? m_closeAsyncLog;
    public TcSettings.TcSettings Settings { get; }
    public MetatagSchema MetatagSchema { get; }
    public ICache Cache { get; private set; }
    public ICatalog Catalog { get; private set; }
    public void CloseAsyncLog(bool skipClose) => m_closeAsyncLog?.Invoke(skipClose); 

    public void RefreshMetatagSchema()
    {
        MetatagSchema.ReplaceFromService(ServiceInterop.GetMetatagSchema());
    }

    public void OverrideCache(ICache cache)
    {
        Cache = cache;
    }

    public void OverrideCatalog(ICatalog catalog)
    {
        Catalog = catalog;
    }

    public AppState(CloseAsyncLogDelegate closeAsyncLogDelegate)
    {
        Settings = new TcSettings.TcSettings();
        Catalog = new Catalog();
        MetatagSchema = new MetatagSchema();
        Cache = new Cache(Settings);
        m_closeAsyncLog = closeAsyncLogDelegate;
    }

    public void RegisterWindowPlace(Window window, string key)
    {
        ((App)Application.Current).WindowPlace.Register(window, key);
    }
}
