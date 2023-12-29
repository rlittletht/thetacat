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
            MainWindow._AppState.RegisterWindowPlace(this, "asyncLogView");
            AsyncLogView.SetAutoscroll();
        }

        void OnClosing(object sender, EventArgs e)
        {
            AsyncLogView.UnsetAutoscroll();
            MainWindow._AppState.CloseAsyncLogMonitor(true);
        }
    }
}
