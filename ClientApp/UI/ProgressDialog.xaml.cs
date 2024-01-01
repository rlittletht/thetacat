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

namespace Thetacat.UI;

public interface IProgressReport
{
    public void UpdateProgress(int  progress);
}

/// <summary>
/// Interaction logic for ProgressDialog.xaml
/// </summary>
public partial class ProgressDialog : Window, IProgressReport
{
    private int ProgressValue = 0;

    public void UpdateProgress(int progress)
    {
        if (ProgressValue != progress)
            ProgressValue = progress;
    }

    public ProgressDialog()
    {
        InitializeComponent();
    }
}