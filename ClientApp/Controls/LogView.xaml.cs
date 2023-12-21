﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
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
using Meziantou.Framework.WPF.Collections;
using Thetacat.Logging;
using Thetacat.Types.Parallel;

namespace Thetacat.Controls
{
    /// <summary>
    /// Interaction logic for LogView.xaml
    /// </summary>
    public partial class LogView : UserControl
    {
#region SortAdorner Support

        private GridViewColumnHeader? sortCol = null;
        private SortAdorner? sortAdorner;

        public void Sort(ListView listView, GridViewColumnHeader? column)
        {
            if (column == null)
                return;

            string sortBy = column.Tag?.ToString() ?? string.Empty;

            if (sortAdorner != null && sortCol != null)
            {
                AdornerLayer.GetAdornerLayer(sortCol)?.Remove(sortAdorner);
                listView.Items.SortDescriptions.Clear();
            }

            ListSortDirection newDir = ListSortDirection.Ascending;
            if (sortCol == column && sortAdorner?.Direction == newDir)
                newDir = ListSortDirection.Descending;

            sortCol = column;
            sortAdorner = new SortAdorner(sortCol, newDir);
            AdornerLayer.GetAdornerLayer(sortCol)?.Add(sortAdorner);
            listView.Items.SortDescriptions.Add(new SortDescription(sortBy, newDir));
        }

        private void SortType(object sender, RoutedEventArgs e)
        {
            Sort(LogEntries, sender as GridViewColumnHeader);
        }
#endregion

        void ScrollToBottom(object? sender, NotifyCollectionChangedEventArgs e)
        {
            return;

            lock (((ICollection)LogEntries.ItemsSource).SyncRoot)
            {
                LogEntries.Items.MoveCurrentToLast();
                LogEntries.ScrollIntoView(LogEntries.Items[LogEntries.Items.Count - 1]);
            }
        }

        public void SetAutoscroll()
        {
            ((INotifyCollectionChanged)LogEntries.ItemsSource).CollectionChanged += ScrollToBottom;
        }

        public void UnsetAutoscroll()
        {
            ((INotifyCollectionChanged)LogEntries.ItemsSource).CollectionChanged -= ScrollToBottom;
        }

        public LogView()
        {
            InitializeComponent();
        }

//        public void ShowCount(object sender, RoutedEventArgs e)
//        {
//            //            MessageBox.Show($"Total count: {LogEntries.Items.Count}, ItemsSourceCount: {((ObservableImmutableList<ILogEntry>)LogEntries.ItemsSource).Count}");
//            MessageBox.Show($"Total count: {LogEntries.Items.Count}, ItemsSourceCount: {((ConcurrentObservableCollection<ILogEntry>)LogEntries.ItemsSource).Count}");
//        }
    }
}
