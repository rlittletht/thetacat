﻿using System;
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