using System.Windows;
using Thetacat.Model;
using Thetacat.ServiceClient;

namespace Thetacat.Types;

public class AppState : IAppState
{
    public TcSettings.TcSettings Settings { get; set; }
    public MetatagSchema? MetatagSchema { get; set; }

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
