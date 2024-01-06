﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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
    public void UpdateProgress(double progress);
    public void WorkCompleted();
}

// This will perform the given work on a background thread
// and launch the modal progress dialog

public delegate void WorkDelegate(IProgressReport reportProgress);


/// <summary>
/// Interaction logic for ProgressDialog.xaml
/// </summary>
public partial class ProgressDialog : Window, IProgressReport
{
    private ProgressDialogModel Model = new();

    public void UpdateProgress(double progress)
    {
        int progressVal = (int)Math.Round(progress * 10);

        if (Model.ProgressValue != progressVal)
            Model.ProgressValue = progressVal;
    }

    public void WorkCompleted()
    {
        Model.ProgressValue = 1000;
        Application.Current.Dispatcher.Invoke(Close);
    }

    public ProgressDialog()
    {
        InitializeComponent();
        DataContext = Model;
    }

    public static void DoWorkWithProgress(WorkDelegate work, Window? parent = null)
    {
        ProgressDialog dialog = new ProgressDialog();

        Task.Run(() => work(dialog));
        dialog.Owner = parent;
        dialog.ShowDialog();
    }
}