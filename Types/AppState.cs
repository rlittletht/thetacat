namespace Thetacat.Types;

public class AppState : IAppState
{
    public TcSettings.TcSettings Settings { get; set; }

    public AppState()
    {
        Settings = TcSettings.TcSettings.LoadSettings();
    }
}
