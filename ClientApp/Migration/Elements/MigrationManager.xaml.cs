﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Thetacat.Migration.Elements;
using Thetacat.Migration.Elements.Metadata;
using Thetacat.ServiceClient.LocalService;
using Thetacat.Types;

namespace Thetacat.Migration.Elements;


/// <summary>
/// Interaction logic for MigrationManager.xaml
/// </summary>
public partial class MigrationManager : Window
{
    private readonly IAppState m_appState;
    private MetatagMigrate m_migrate;

    void BuildMetadataReportFromDatabase(string database)
    {
        ElementsDb db = ElementsDb.Create(database);

        MediaMigrationTab.Initialize(m_appState, db, m_migrate);
        MetatagMigrationTab.Initialize(m_appState, db, m_migrate);
        MetadataMigrationTab.Initialize(m_appState, db, m_migrate);

        db.Close();
    }

    public MigrationManager(string database, IAppState appState)
    {
        m_appState = appState;
        InitializeComponent();

        m_migrate = new MetatagMigrate();
        BuildMetadataReportFromDatabase(database);
        m_appState.RegisterWindowPlace(this, "ElementsMigrationManager");
    }
}
