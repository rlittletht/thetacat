using System.Windows;
using Thetacat.Model;
using Thetacat.Model.Metatags;
using Thetacat.ServiceClient;

namespace Thetacat.Types;

public class AppState : IAppState
{
    public TcSettings.TcSettings Settings { get; }
    public MetatagSchema MetatagSchema { get; }
    public ICache Cache { get; private set; }

    public Catalog Catalog { get; }

    public void RefreshMetatagSchema()
    {
        MetatagSchema.ReplaceFromService(ServiceInterop.GetMetatagSchema());
    }

    public void OverrideCache(ICache cache)
    {
        Cache = cache;
    }

    public AppState()
    {
        Settings = new TcSettings.TcSettings();
        Catalog = new Catalog();
        MetatagSchema = new MetatagSchema();
        Cache = new Cache(Settings);
    }

    public void RegisterWindowPlace(Window window, string key)
    {
        ((App)Application.Current).WindowPlace.Register(window, key);
    }
}
