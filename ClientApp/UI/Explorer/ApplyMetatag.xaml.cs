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
using Thetacat.Model.Metatags;
using Thetacat.Standards;

namespace Thetacat.UI.Explorer
{
    /// <summary>
    /// Interaction logic for ApplyMetatag.xaml
    /// </summary>
    public partial class ApplyMetatag : Window
    {
        private ApplyMetatagModel model = new();

        public ApplyMetatag()
        {
            InitializeComponent();
            DataContext = model;
        }

        private void DoSave(object sender, RoutedEventArgs e)
        {

        }
    }
}
