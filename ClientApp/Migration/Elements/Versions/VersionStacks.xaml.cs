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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Thetacat.Migration.Elements.Media;
using Thetacat.Migration.Elements.Metadata.UI;

namespace Thetacat.Migration.Elements.Versions
{
    /// <summary>
    /// Interaction logic for VersionStacks.xaml
    /// </summary>
    public partial class VersionStacks : UserControl
    {
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
        }

        public void Initialize(ElementsDb db, ElementsMigrate migrate)
        {
            m_migrate = migrate;
            _Migrate.StacksMigrate.SetVersionStacks(new List<PseVersionStackItem>(db.ReadVersionStacks()));
            StackListView.ItemsSource = _Migrate.StacksMigrate.Stacks;
        }
    }
}
