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

        public static void OnMainWindowCreated()
        {
            ((App)Current).m_appState?.LoadProfiles();
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
            // NOTE: YOU CANNOT BRING UP ANY UI until we have created the main window
            // (if you bring up UI like a messagebox, this dispatcher will run the pending
            // items in the dispatch queue, which includes creating the main window, but
            // that will fail if we haven't finished initializing the App here (namely, the
            // StartupUri has to get setup).

            try
            {
                m_appState = new AppState();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not create app state: {ex}");
                Shutdown(-1);
            }

            string? directory = Path.GetDirectoryName(SettingsPath);

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

        public static string ClientDatabasePath(string ClientDatabaseName)
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), $"thetacat\\{ClientDatabaseName}");
        }
        
    }
}
