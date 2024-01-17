﻿using System;
using System.ComponentModel;
using System.IO;
using System.Windows.Controls;
using System.Windows.Forms;
using Thetacat.Model;
using Thetacat.ServiceClient;
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

    public CacheConfig()
    {
        InitializeComponent();
        DataContext = _Model;
        Workgroup.ItemsSource = _Model.Workgroups;
        _Model.PropertyChanged += ModelPropertyChanged;
    }

    public void LoadFromSettings()
    {
        _Model.CacheLocation = App.State.Settings.CacheLocation ?? string.Empty;
        _Model.DerivativeLocation = App.State.Settings.DerivativeCache ?? string.Empty;
        _Model.SetCacheTypeFromString(App.State.Settings.CacheType ?? string.Empty);
        if (App.State.Settings.WorkgroupId != null)
        {
            _Model.WorkgroupID = App.State.Settings.WorkgroupId;
            _Model.PopulateWorkgroups();
            _Model.SetWorkgroup(Guid.Parse(_Model.WorkgroupID));
            try
            {
                ServiceWorkgroup workgroup = ServiceInterop.GetWorkgroupDetails(Guid.Parse(_Model.WorkgroupID));
                
                _Model.WorkgroupName = workgroup.Name ?? string.Empty;
                _Model.WorkgroupCacheRoot = workgroup.CacheRoot ?? string.Empty;
                _Model.WorkgroupServerPath = workgroup.ServerPath ?? string.Empty;
            }
            catch (Exception)
            {
                _Model.WorkgroupName = App.State.Settings.WorkgroupName ?? String.Empty;
                _Model.WorkgroupCacheRoot = App.State.Settings.WorkgroupCacheRoot ?? String.Empty;
                _Model.WorkgroupServerPath = App.State.Settings.WorkgroupCacheServer ?? String.Empty;
            }
        }
    }


    private void ModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == "CurrentCacheType")
        {
            Cache.CacheType cacheType = _Model.CurrentCacheType.Type;

            if (WorkgroupOptions != null)
                WorkgroupOptions.IsEnabled = cacheType == Cache.CacheType.Workgroup;
            if (LocalOptions != null)
                LocalOptions.IsEnabled = cacheType == Cache.CacheType.Private;
            if (cacheType != Cache.CacheType.Private)
                _Model.PopulateWorkgroups();

            return;
        }

        if (e.PropertyName == "CurrentWorkgroup")
            _Model.SetWorkgroup(_Model.CurrentWorkgroup?.Workgroup?.ID);
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

        string fullPath = PathSegment.Join(_Model.WorkgroupServerPath, _Model.WorkgroupCacheRoot).Local;

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
        Cache.CacheType cacheType = Cache.CacheTypeFromString(CacheConfiguration.Text);

        PathSegment derivativeLocation = new PathSegment(_Model.DerivativeLocation);
        try
        {
            PathSegment formatsDirectory = PathSegment.Join(derivativeLocation, "cat-derivatives/formats");
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
            MessageBox.Show($"can't create or use derivative location {derivativeLocation}: {ex}");
            return false;
        }

        App.State.Settings.DerivativeCache = derivativeLocation;
        App.State.Settings.CacheLocation = _Model.CacheLocation;

        App.State.Settings.CacheType = Cache.StringFromCacheType(cacheType);

        if (cacheType == Cache.CacheType.Private)
        {
            App.State.Settings.WorkgroupId = null;
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

            App.State.Settings.WorkgroupId = workgroup.ID.ToString();
        }
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
                    _Model.SetWorkgroup(id.Value);
                }
            }
            Cache.CacheType cacheType = Cache.CacheTypeFromString(selected ?? "private");

            if (WorkgroupOptions != null)
                WorkgroupOptions.IsEnabled = cacheType == Cache.CacheType.Workgroup;
            if (LocalOptions != null)
                LocalOptions.IsEnabled = cacheType == Cache.CacheType.Private;
            if (cacheType != Cache.CacheType.Private)
                _Model.PopulateWorkgroups();
        }
    }
}
