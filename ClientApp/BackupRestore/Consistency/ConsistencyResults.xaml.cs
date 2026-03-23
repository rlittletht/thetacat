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
using Thetacat.Model;
using Thetacat.UI.Controls.MediaItemsListControl;
using Thetacat.Util;

namespace Thetacat.BackupRestore.Consistency
{
    /// <summary>
    /// Interaction logic for ConsistencyResults.xaml
    /// </summary>
    public partial class ConsistencyResults : Window
    {
        private ConsistencyResultsModel Model = new();

        public ConsistencyResults()
        {
            DataContext = Model;
            InitializeComponent();
        }

        public static void ShowResults(List<ConsistencyResult> results)
        {
            ConsistencyResults resultsDialog = new();

            resultsDialog.Model.Results.AddRange(results);

            resultsDialog.ShowDialog();
        }
    }
}
