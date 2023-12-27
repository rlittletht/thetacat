﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using Thetacat.Migration.Elements.Media;
using Thetacat.Migration.Elements.Metadata.UI;
using Thetacat.Util;

namespace Thetacat.Migration.Elements.Versions
{
    /// <summary>
    /// Interaction logic for VersionStacks.xaml
    /// </summary>
    public partial class VersionStacks : UserControl
    {
        private readonly SortableListViewSupport m_sortableListViewSupport;
        private void SortType(object sender, RoutedEventArgs e) => m_sortableListViewSupport.Sort(sender as GridViewColumnHeader);
        private ElementsMigrate? m_migrate;

        private ElementsMigrate _Migrate
        {
            get
            {
                if (m_migrate == null)
                    throw new Exception($"initialize never called on {this.GetType().Name}");
                return m_migrate;
            }
        }

        public VersionStacks()
        {
            InitializeComponent();
            m_sortableListViewSupport = new SortableListViewSupport(diffOpListView);
            diffOpListView.ItemsSource = m_migrateSummaryItems;
        }

        public void Initialize(ElementsDb db, ElementsMigrate migrate)
        {
            m_migrate = migrate;
            _Migrate.StacksMigrate.SetVersionStacks(new List<PseStackItem>(db.ReadVersionStacks()));
            _Migrate.StacksMigrate.SetMediaStacks(new List<PseStackItem>(db.ReadMediaStacks()));
            VersionStackListView.ItemsSource = _Migrate.StacksMigrate.VersionStacks;
            MediaStackListView.ItemsSource = _Migrate.StacksMigrate.MediaStacks;
        }

        private void DoMigrate(object sender, RoutedEventArgs e)
        {
            // associate every version stack with its cat media item
            _Migrate.StacksMigrate.CreateCatStacks(_Migrate.MediaMigrate);
        }

        private ObservableCollection<StackMigrateSummaryItem> m_migrateSummaryItems = new();

        private void DoSummaryKeyDown(object sender, KeyEventArgs e) => CheckableListViewSupport<StackMigrateSummaryItem>.DoKeyDown(diffOpListView, sender, e);
    }
}
