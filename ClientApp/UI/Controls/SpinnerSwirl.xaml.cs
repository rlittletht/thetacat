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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Thetacat.Util;

namespace Thetacat.UI.Controls;

/// <summary>
/// Interaction logic for SpinnerSwirl.xaml
/// </summary>
public partial class SpinnerSwirl : UserControl
{
    public SpinnerSwirl()
    {
        InitializeComponent();
    }

    public void Start()
    {
        ThreadContext.InvokeOnUiThread(
            () =>
            {
                Visibility = Visibility.Visible;
                ((Storyboard?)Resources.FindName("spinner"))?.Begin();
            });
    }

    public void Stop()
    {
        ThreadContext.InvokeOnUiThread(
            () =>
            {
                Visibility = Visibility.Collapsed;
                ((Storyboard?)Resources.FindName("spinner"))?.Stop();
            });
    }
}