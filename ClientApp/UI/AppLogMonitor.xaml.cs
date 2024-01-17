using System;
using System.Windows;
using System.Windows.Data;

namespace Thetacat.UI
{
    /// <summary>
    /// Interaction logic for AsyncLog.xaml
    /// </summary>
    public partial class AppLogMonitor : Window
    {
        readonly object lockObject = new object();

        public AppLogMonitor()
        {
            InitializeComponent();
            AppLogView.LogEntries.ItemsSource = MainWindow._AppLog.Entries.AsObservable;
            BindingOperations.EnableCollectionSynchronization(AppLogView.LogEntries.ItemsSource, lockObject);
            System.Diagnostics.PresentationTraceSources.SetTraceLevel(
                AppLogView.LogEntries.ItemContainerGenerator,
                System.Diagnostics.PresentationTraceLevel.High);
            App.State.RegisterWindowPlace(this, "appLogView");
            AppLogView.SetAutoscroll();
        }

        void OnClosing(object sender, EventArgs e)
        {
            AppLogView.UnsetAutoscroll();
            App.State.CloseAppLogMonitor(true);
        }
    }
}
