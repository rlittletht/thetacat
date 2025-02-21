using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Thetacat.Model.Caching;
using Thetacat.ServiceClient;
using Thetacat.Types;
using Thetacat.Util;
using MessageBox = System.Windows.Forms.MessageBox;
using UserControl = System.Windows.Controls.UserControl;

namespace Thetacat.UI.Options;

/// <summary>
/// Interaction logic for CacheConfig.xaml
/// </summary>
public partial class CacheConfig : UserControl
{
    readonly CacheConfigModel _Model = new CacheConfigModel();
    AccountModel? _AccountModel;

    public CacheConfig()
    {
        InitializeComponent();
        DataContext = _Model;
        Workgroup.ItemsSource = _Model.Workgroups;
        _Model.PropertyChanged += ModelPropertyChanged;
    }

    public void LoadFromSettings(AccountModel accountModel, CatOptionsModel catOptionsModel, string sqlConnection, Guid catalogID)
    {
        _AccountModel = accountModel;
        _Model.ProfileOptions = catOptionsModel.CurrentProfile;

        _Model.CacheLocation = _Model.ProfileOptions?.Profile.CacheLocation ?? string.Empty;
        _Model.LocalCatalogCacheLocation = _Model.ProfileOptions?.Profile.LocalCatalogCache ?? string.Empty;

        // we might have changed the sql server connection string, so use that string
        App.State.PushTemporarySqlConnection(sqlConnection);
        if (_Model.ProfileOptions?.Profile.WorkgroupId != null)
        {
            string workgroupId = _Model.ProfileOptions?.Profile.WorkgroupId!;
            _Model.PopulateWorkgroups(catalogID);
            _Model.SetWorkgroup(_AccountModel, Guid.Parse(workgroupId));
            try
            {
                ServiceWorkgroup workgroup = ServiceInterop.GetWorkgroupDetails(catalogID, Guid.Parse(_Model.WorkgroupID));

                _Model.WorkgroupName = workgroup.Name ?? string.Empty;
                _Model.WorkgroupCacheRoot = workgroup.CacheRoot ?? string.Empty;
                _Model.WorkgroupServerPath = workgroup.ServerPath ?? string.Empty;
            }
            catch (Exception)
            {
                _Model.WorkgroupName = _Model.ProfileOptions?.Profile.WorkgroupName ?? String.Empty;
                _Model.WorkgroupCacheRoot = _Model.ProfileOptions?.Profile.WorkgroupCacheRoot ?? String.Empty;
                _Model.WorkgroupServerPath = _Model.ProfileOptions?.Profile.WorkgroupCacheServer ?? String.Empty;
            }
        }

        App.State.PopTemporarySqlConnection();
    }


    private void ModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // this was on a profile type change...
//            if (cacheType != Cache.CacheType.Private)
//            {
//                App.State.PushTemporarySqlConnection(_AccountModel?.SqlConnection ?? "");
//                _Model.PopulateWorkgroups(_AccountModel?.CatalogDefinition?.ID ?? Guid.Empty);
//                App.State.PopTemporarySqlConnection();
//            }
//            return;
//        }

        if (e.PropertyName == "CurrentWorkgroup")
            _Model.SetWorkgroup(_AccountModel, _Model.CurrentWorkgroup?.Workgroup?.ID);

        if (e.PropertyName == "CreateNewWorkgroup")
        {
            if (_Model.CreateNewWorkgroup)
            {
                _Model.WorkgroupID = RT.Comb.Provider.Sql.Create().ToString();
            }
            else
            {
                _Model.WorkgroupID = _Model.CurrentWorkgroup?.Workgroup?.ID.ToString() ?? Guid.Empty.ToString();
            }
        }
    }


    public bool FSaveSettings(string sqlConnection, Guid catalogID)
    {
        PathSegment localCatalogCacheRoot = new PathSegment(_Model.LocalCatalogCacheLocation);
        try
        {
            PathSegment formatsDirectory = PathSegment.Join(localCatalogCacheRoot, "cat-derivatives/formats");
            // other derivatives may exist but they will create their directories on demand

            if (!Directory.Exists(formatsDirectory.Local))
            {
                Directory.CreateDirectory(formatsDirectory.Local);
                if (!Directory.Exists(formatsDirectory.Local))
                {
                    throw new CatExceptionInternalFailure("directory didn't exist after create");
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"can't create or use derivative location {localCatalogCacheRoot}: {ex}");
            return false;
        }

        _Model.ProfileOptions!.Profile.LocalCatalogCache = localCatalogCacheRoot;
        _Model.ProfileOptions.Profile.CacheLocation = _Model.CacheLocation;

        // verify workgroup settings are valid
        bool valid = CreateWorkgroup.ValidateValidWorkgroupSettings(_Model.WorkgroupName, _Model.WorkgroupServerPath, _Model.WorkgroupCacheRoot);

        if (!valid)
            return false;

        string newId;

        if (_Model.CreateNewWorkgroup)
        {
            CreateWorkgroup.CreateNewWorkgroup(
                catalogID,
                sqlConnection,
                _Model.WorkgroupID,
                _Model.WorkgroupName,
                _Model.WorkgroupServerPath,
                _Model.WorkgroupCacheRoot);

            newId = _Model.WorkgroupID;
        }
        else
        {
            ServiceWorkgroup workgroup =
                new()
                {
                    Name = _Model.WorkgroupName,
                    ServerPath = PathSegment.CreateFromString(_Model.WorkgroupServerPath),
                    CacheRoot = PathSegment.CreateFromString(_Model.WorkgroupCacheRoot)
                };

            Guid id;

            if (!Guid.TryParse(_Model.WorkgroupID, out id))
            {
                MessageBox.Show($"invalid workgroup id. can't save workgroup: {_Model.WorkgroupID}");
                return false;
            }

            App.State.PushTemporarySqlConnection(sqlConnection);
            workgroup.ID = id;
            ServiceInterop.UpdateWorkgroup(catalogID, workgroup);
            App.State.PopTemporarySqlConnection();

            newId = workgroup.ID!.Value.ToString();
        }

        _Model.ProfileOptions.Profile.WorkgroupId = newId;

        return true;
    }

    private void ChangeWorkgroup(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count > 0 && e.AddedItems[0] is string)
        {
            string? selected = (string?)e.AddedItems[0];

            if (selected != null)
            {
                Guid? id = _Model.GetWorkgroupIdFromName(selected);

                if (id != null)
                {
                    _Model.SetWorkgroup(_AccountModel, id.Value);
                }
            }

//            Cache.CacheType cacheType = Cache.CacheTypeFromString(selected ?? "private");
//
//            if (WorkgroupOptions != null)
//                WorkgroupOptions.IsEnabled = cacheType == Cache.CacheType.Workgroup;
//            if (LocalOptions != null)
//                LocalOptions.IsEnabled = cacheType == Cache.CacheType.Private;
//            if (cacheType != Cache.CacheType.Private)
//            {
//                App.State.PushTemporarySqlConnection(_AccountModel?.SqlConnection ?? "");
//                _Model.PopulateWorkgroups(_AccountModel?.CatalogDefinition?.ID ?? Guid.Empty);
//                App.State.PopTemporarySqlConnection();
//            }
//        }
        }
    }

    private void CreateNewWorkgroup(object sender, System.Windows.RoutedEventArgs e)
    {
        ServiceWorkgroup? newWorkgroup = CreateWorkgroup.Create(Window.GetWindow(this));

        if (newWorkgroup != null)
        {
            // we are creating a new workgroup
            _Model.CreateNewWorkgroup = true;
            _Model.WorkgroupName = newWorkgroup.Name!;
            _Model.WorkgroupServerPath = newWorkgroup.ServerPath!;
            _Model.WorkgroupCacheRoot = newWorkgroup.CacheRoot!;
            _Model.WorkgroupID = newWorkgroup.ID!.Value.ToString();
            _Model.CreateNewWorkgroup = true;
        }
    }
}
