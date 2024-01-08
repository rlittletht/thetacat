using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using RestoreWindowPlace;
using Thetacat.Types;

namespace Thetacat
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static IAppState State => ((App)Current)._State;

        public IAppState _State
        {
            get
            {
                if (m_appState == null)
                    throw new CatExceptionInitializationFailure("app state not initialized. called too early?");

                return m_appState;
            }
        }

        private readonly AppState? m_appState;

        public WindowPlace WindowPlace { get; }

        public App()
        {
            WindowPlace = new WindowPlace("placement.config");

            m_appState = new AppState();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            WindowPlace.Save();
        }
    }
}
