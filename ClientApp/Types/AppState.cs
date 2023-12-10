using System.Windows;
using Thetacat.Model;
using Thetacat.Model.Metatags;
using Thetacat.ServiceClient;

namespace Thetacat.Types;

public class AppState : IAppState
{
    private Catalog? m_catalog;

    public TcSettings.TcSettings Settings { get; set; }
    public MetatagSchema? MetatagSchema { get; set; }

    public Catalog Catalog
    {
        get => m_catalog ??= new Catalog();
        set => m_catalog = value;
    }

    public void RefreshMetatagSchema()
    {
        MetatagSchema = ServiceInterop.GetMetatagSchema();
    }

    public AppState()
    {
        Settings = TcSettings.TcSettings.LoadSettings();
    }

    public void RegisterWindowPlace(Window window, string key)
    {
        ((App)Application.Current).WindowPlace.Register(window, key);
    }
}
