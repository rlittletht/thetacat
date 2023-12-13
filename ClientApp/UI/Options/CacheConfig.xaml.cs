using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Thetacat.ServiceClient;
using Thetacat.Standards;
using Thetacat.Types;
using Thetacat.Util;
using UserControl = System.Windows.Controls.UserControl;

namespace Thetacat.UI.Options;

/// <summary>
/// Interaction logic for CacheConfig.xaml
/// </summary>
public partial class CacheConfig : UserControl
{
    readonly CacheConfigModel _Model = new CacheConfigModel();
    private IAppState? m_appState;

    private IAppState _AppState
    {
        get
        {
            if (m_appState == null)
                throw new Exception($"initialize never called on {this.GetType().Name}");
            return m_appState;
        }
    }

    public enum CacheType
    {
        Private,
        Workgroup,
        Unknown
    }

    CacheType CacheTypeFromString(string value)
    {
        if (String.Compare(value, "private", StringComparison.InvariantCultureIgnoreCase) == 0)
            return CacheType.Private;
        else if (String.Compare(value, "workgroup", StringComparison.InvariantCultureIgnoreCase) == 0)
            return CacheType.Workgroup;

        return CacheType.Unknown;
    }

    string StringFromCacheType(CacheType cacheType)
    {
        switch (cacheType)
        {
            case CacheType.Private:
                return "private";
            case CacheType.Workgroup:
                return "workgroup";
        }

        throw new ArgumentException("bad cache type argument");
    }

    public CacheConfig()
    {
        InitializeComponent();
        DataContext = _Model;
    }

    public void Initialize(IAppState appState)
    {
        m_appState = appState;
    }


    public void LoadFromSettings()
    {
        _Model.CacheLocation = _AppState.Settings.CacheLocation ?? string.Empty;
        _Model.CacheType = _AppState.Settings.CacheType ?? string.Empty;
        if (_AppState.Settings.WorkgroupId != null)
        {
            _Model.WorkgroupID = _AppState.Settings.WorkgroupId;

            try
            {
                ServiceWorkgroup workgroup = ServiceInterop.GetWorkgroupDetails(Guid.Parse(_Model.WorkgroupID));

                _Model.WorkgroupName = workgroup.Name ?? string.Empty;
                _Model.WorkgroupCacheRoot = workgroup.CacheRoot ?? string.Empty;
                _Model.WorkgroupServerPath = workgroup.ServerPath ?? string.Empty;
            }
            catch (Exception)
            {
                _Model.WorkgroupName = _AppState.Settings.WorkgroupName ?? String.Empty;
                _Model.WorkgroupCacheRoot = _AppState.Settings.WorkgroupCacheRoot ?? String.Empty;
                _Model.WorkgroupServerPath = _AppState.Settings.WorkgroupCacheServer ?? String.Empty;
            }
        }
    }

    private void ChangeCacheType(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count > 0 && e.AddedItems[0] is ComboBoxItem)
        {
            ComboBoxItem? item = (ComboBoxItem?)e.AddedItems[0];
            string? selected = (string?)item?.Content;

            CacheType cacheType = CacheTypeFromString(selected ?? "private");

            if (WorkgroupOptions != null)
                WorkgroupOptions.IsEnabled = cacheType == CacheType.Workgroup;
            if (LocalOptions != null)
                LocalOptions.IsEnabled = cacheType == CacheType.Private;
            if (cacheType != CacheType.Private)
                _Model.PopulateWorkgroups();
        }
    }

    bool IsValidWorkgroupSettings()
    {
        if (string.IsNullOrEmpty(_Model.WorkgroupName))
        {
            MessageBox.Show("Can't save Workgroup information. Workgroup name not set");
            return false;
        }

        // we are going to create a workgroup
        if (string.IsNullOrEmpty(_Model.WorkgroupServerPath))
        {
            MessageBox.Show("Can't save workgroup information. Server path not set");
            return false;
        }

        if (string.IsNullOrEmpty(_Model.WorkgroupCacheRoot))
        {
            MessageBox.Show("Can't save workgroup information. Cache root not set");
            return false;
        }

        string fullPath = PathSegment.Combine(_Model.WorkgroupServerPath, _Model.WorkgroupCacheRoot).Local;

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

    public bool FSaveSettings()
    {
        CacheType cacheType = CacheTypeFromString(CacheConfiguration.Text);

        _AppState.Settings.CacheLocation = _Model.CacheLocation;

        if (cacheType == CacheType.Private)
        {
            _AppState.Settings.WorkgroupId = null;
            _AppState.Settings.CacheType = StringFromCacheType(cacheType);
        }
        else
        {
            // verify workgroup settings are valid
            bool valid = IsValidWorkgroupSettings();

            if (!valid)
                return false;

            ServiceWorkgroup workgroup =
                new ServiceWorkgroup()
                {
                    Name = _Model.WorkgroupName,
                    ServerPath = PathSegment.CreateFromString(_Model.WorkgroupServerPath),
                    CacheRoot = PathSegment.CreateFromString(_Model.WorkgroupCacheRoot)
                };

            if (string.IsNullOrEmpty(_Model.WorkgroupID))
            {
                workgroup.ID = Guid.NewGuid();
                ServiceInterop.CreateWorkgroup(workgroup);
            }
            else
            {
                Guid id;

                if (!Guid.TryParse(_Model.WorkgroupID, out id))
                {
                    MessageBox.Show($"invalid workgroup id. can't save workgroup: {_Model.WorkgroupID}");
                    return false;
                }

                workgroup.ID = id;
                ServiceInterop.UpdateWorkgroup(workgroup);
            }

            _AppState.Settings.WorkgroupId = workgroup.ID.ToString();
        }
        return true;
    }
}
