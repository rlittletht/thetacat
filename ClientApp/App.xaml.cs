using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
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

        void ReplacePlacements(Dictionary<string, Rectangle> from, Dictionary<string, Rectangle> to)
        {
            to.Clear();
            foreach (KeyValuePair<string, Rectangle> pair in from)
            {
                to.Add(pair.Key, new Rectangle(pair.Value.X, pair.Value.Y, pair.Value.Width, pair.Value.Height));
            }
        }

        private Dictionary<string, Rectangle> LoadPlacements()
        {
            Dictionary<string, Rectangle> placements = new Dictionary<string, Rectangle>();

            ReplacePlacements(App.State.Settings.Placements, placements);
            return placements;
        }

        void SavePlacements(Dictionary<string, Rectangle> placements)
        {
            ReplacePlacements(placements, App.State.Settings.Placements);
            App.State.Settings.WriteSettings();
        }

        public App()
        {
            m_appState = new AppState();

            string? directory = Path.GetDirectoryName(SettingsPath);

            if (directory != null)
                Directory.CreateDirectory(directory);

            directory = Path.GetDirectoryName(ClientDatabasePath);
            if (directory != null)
                Directory.CreateDirectory(directory);

            WindowPlace = new WindowPlace(LoadPlacements, SavePlacements);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            WindowPlace.Save();
        }

        public static string SettingsPath => 
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "thetacat\\options.xml");

        public static string ClientDatabasePath => 
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "thetacat\\client.db");
    }
}
