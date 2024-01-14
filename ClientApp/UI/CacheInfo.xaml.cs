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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Thetacat.UI
{
    /// <summary>
    /// Interaction logic for CacheInfo.xaml
    /// </summary>
    public partial class CacheInfo : Window
    {
        private CacheInfoModel m_model = new();

        public CacheInfo()
        {
            InitializeComponent();
            DataContext = m_model;
            App.State.RegisterWindowPlace(this, "cache-info");
        }
    }
}
