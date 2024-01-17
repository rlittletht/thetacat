using System.Windows;

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
