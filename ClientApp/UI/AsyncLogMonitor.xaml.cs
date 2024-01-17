using System;
using System.Windows;
using System.Windows.Data;

namespace Thetacat.UI
{
    /// <summary>
    /// Interaction logic for AsyncLogMonitor.xaml
    /// </summary>
    public partial class AsyncLogMonitor : Window
    {
        readonly object lockObject = new object();

        public AsyncLogMonitor()
        {
            InitializeComponent();
            AsyncLogView.LogEntries.ItemsSource = MainWindow._AsyncLog.Entries.AsObservable;
            BindingOperations.EnableCollectionSynchronization(AsyncLogView.LogEntries.ItemsSource, lockObject);
            System.Diagnostics.PresentationTraceSources.SetTraceLevel(
                AsyncLogView.LogEntries.ItemContainerGenerator,
                System.Diagnostics.PresentationTraceLevel.High);
            App.State.RegisterWindowPlace(this, "asyncLogView");
            AsyncLogView.SetAutoscroll();
        }

        void OnClosing(object sender, EventArgs e)
        {
            AsyncLogView.UnsetAutoscroll();
            App.State.CloseAsyncLogMonitor(true);
        }
    }
}
