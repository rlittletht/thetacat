using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using Thetacat.Model;
using Thetacat.ServiceClient;
using Thetacat.Util;

namespace Thetacat.UI.Options;

/// <summary>
/// Interaction logic for CreateWorkgroup.xaml
/// </summary>
public partial class CreateWorkgroup : Window
{
    readonly CreateWorkgroupModel m_model = new();

    public CreateWorkgroup()
    {
        DataContext = m_model;
        InitializeComponent();
    }

    private void DoCancel(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void DoSaveWorkgroup(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    public static ServiceWorkgroup? Create(Window? owner)
    {
        CreateWorkgroup dialog = new();
        dialog.Owner = owner;
        dialog.m_model.WorkgroupID = RT.Comb.Provider.Sql.Create().ToString();

        if (dialog.ShowDialog() == true)
        {
            return new ServiceWorkgroup
                   {
                       ID = Guid.Parse(dialog.m_model.WorkgroupID),
                       Name = dialog.m_model.WorkgroupName,
                       ServerPath = dialog.m_model.WorkgroupServerPath,
                       CacheRoot = dialog.m_model.WorkgroupCacheRoot
                   };
        }

        return null;
    }

    public static bool ValidateValidWorkgroupSettings(string workgroupName, string serverPath, string cacheRoot)
    {
        if (string.IsNullOrEmpty(workgroupName))
        {
            MessageBox.Show("Can't save Workgroup information. Workgroup name not set");
            return false;
        }

        // we are going to create a workgroup
        if (string.IsNullOrEmpty(serverPath))
        {
            MessageBox.Show("Can't save workgroup information. Server path not set");
            return false;
        }

        if (string.IsNullOrEmpty(cacheRoot))
        {
            MessageBox.Show("Can't save workgroup information. Cache root not set");
            return false;
        }

        string fullPath = PathSegment.Join(serverPath, cacheRoot).Local;

        if (!Path.Exists(fullPath))
        {
            // try to create the path
            try
            {
                Directory.CreateDirectory(fullPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Can't create directory for workgroup cache: {ex.Message}");
                return false;
            }
        }
        else if ((File.GetAttributes(fullPath) & FileAttributes.Directory) != FileAttributes.Directory)
        {
            MessageBox.Show($"Can't create workgroup. Cache is not a directory: {fullPath}");
            return false;
        }

        return true;
    }

    public static void CreateNewWorkgroup(Guid catalogId, string? sqlConnection, string workgroupId, string workgroupName, string serverPath, string cacheRoot)
    {
        ServiceWorkgroup workgroup =
            new()
            {
                ID = Guid.Parse(workgroupId),
                Name = workgroupName,
                ServerPath = PathSegment.CreateFromString(serverPath),
                CacheRoot = PathSegment.CreateFromString(cacheRoot)
            };

        if (sqlConnection != null)
            App.State.PushTemporarySqlConnection(sqlConnection);

        ServiceInterop.CreateWorkgroup(catalogId, workgroup);
        if (sqlConnection != null)
            App.State.PopTemporarySqlConnection();
    }
}
