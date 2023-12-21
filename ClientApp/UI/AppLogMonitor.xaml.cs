using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

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
            MainWindow._AppState.RegisterWindowPlace(this, "appLogView");
            AppLogView.SetAutoscroll();
        }

        void OnClosing(object sender, EventArgs e)
        {
            AppLogView.UnsetAutoscroll();
            MainWindow._AppState.CloseAppLogMonitor(true);
        }
    }
}
