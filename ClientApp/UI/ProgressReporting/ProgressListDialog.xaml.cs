using System.Windows;

namespace Thetacat.UI.ProgressReporting
{
    /// <summary>
    /// Interaction logic for ProgressListDialog.xaml
    /// </summary>
    public partial class ProgressListDialog : Window
    {
        public ProgressListDialog()
        {
            InitializeComponent();
            App.State.RegisterWindowPlace(this, "ProgressListDialog");
        }
    }
}
