using System.Windows;
using Thetacat.Model;
using Thetacat.Model.Metatags;
using Thetacat.ServiceClient;

namespace Thetacat.Types;

public class AppState : IAppState
{
    private readonly Catalog m_catalog;
    private readonly MetatagSchema m_metatagSchema;
    private readonly TcSettings.TcSettings m_settings;

    public TcSettings.TcSettings Settings => m_settings;
    public MetatagSchema MetatagSchema => m_metatagSchema; 
    public Catalog Catalog => m_catalog;

    public void RefreshMetatagSchema()
    {
        m_metatagSchema.ReplaceFromService(ServiceInterop.GetMetatagSchema());

    }

    public AppState()
    {
        m_settings = TcSettings.TcSettings.LoadSettings();
        m_catalog = new Catalog();
        m_metatagSchema = new MetatagSchema();
    }

    public void RegisterWindowPlace(Window window, string key)
    {
        ((App)Application.Current).WindowPlace.Register(window, key);
    }
}
