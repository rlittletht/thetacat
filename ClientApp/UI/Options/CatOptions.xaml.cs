using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using Thetacat.Secrets;
using Thetacat.TcSettings;
using Thetacat.UI.Input;

namespace Thetacat.UI.Options;

/// <summary>
/// Interaction logic for Options.xaml
/// </summary>
public partial class CatOptions : Window
{
    private CatOptionsModel m_model = new();

    public CatOptions()
    {
        InitializeComponent();
        App.State.RegisterWindowPlace(this, "CatOptions");
        foreach (KeyValuePair<string, Profile> pair in App.State.Settings.Profiles)
        {
            ProfileOptions options = new ProfileOptions(pair.Value);
            m_model.ProfileOptions.Add(options);

            if (pair.Value == App.State.ActiveProfile)
                m_model.CurrentProfile = options;
        }

        m_model.PropertyChanged += ModelPropertyChanged;

        DataContext = m_model;
        AccountTab._Model.PropertyChanged += AccountModelPropertyChanged;
        AccountTab.LoadFromSettings(m_model);
        CacheConfigTab.LoadFromSettings(AccountTab._Model, m_model, AppSecrets.MasterSqlConnectionString, AccountTab.CatalogID);


    }

    private void AccountModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == "SqlConnection")
        {
            // if the SqlConnection changed, then we need to reload cache (for workgroups)
            // but first we have to update the list of valid catalogs
            AccountTab.LoadCatalogDefinitions();
            CacheConfigTab.LoadFromSettings(AccountTab._Model, m_model, AccountTab._Model.SqlConnection, AccountTab.CatalogID);
        }

        if (e.PropertyName == "CreateNewCatalog")
        {
            if (AccountTab._Model.CreateNewCatalog)
                AccountTab._Model.CurrentCatalogID = Guid.NewGuid();
            else
                AccountTab._Model.CurrentCatalogID = AccountTab._Model.CatalogDefinition?.ID ?? Guid.Empty;
        }
        else if (e.PropertyName == "CatalogDefinition")
        {
            if (AccountTab._Model.CatalogDefinition != null)
            {
                AccountTab._Model.CatalogName = AccountTab._Model.CatalogDefinition.Name;
                AccountTab._Model.CatalogDescription = AccountTab._Model.CatalogDefinition.Description;
                AccountTab._Model.CreateNewCatalog = false;

                CacheConfigTab.LoadFromSettings(AccountTab._Model, m_model, AccountTab._Model.SqlConnection, AccountTab.CatalogID);
            }
        }
    }

    public void SaveToSettings()
    {
        if (m_model.CurrentProfile == null)
            return;

        if (!CacheConfigTab.FSaveSettings(AccountTab._Model.SqlConnection, AccountTab.CatalogID))
            MessageBox.Show("Failed to save Cache options");
        if (!AccountTab.FSaveSettings())
            MessageBox.Show("Failed to save account options");


        if (m_model.CurrentProfile.Default)
        {
            // unset other profiles' defaultness
            foreach (Profile profile in App.State.Settings.Profiles.Values)
            {
                profile.Default = false;
            }
        }

        m_model.CurrentProfile.Profile.Default = m_model.CurrentProfile.Default;

        // now write the profile back to the settings
        if (!App.State.Settings.Profiles.TryAdd(m_model.CurrentProfile.ProfileName, m_model.CurrentProfile.Profile))
            App.State.Settings.Profiles[m_model.CurrentProfile.ProfileName] = m_model.CurrentProfile.Profile;

        App.State.ChangeProfile(m_model.CurrentProfile.ProfileName);
        App.State.Settings.WriteSettings();
    }

    private void ModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == "CurrentProfile")
        {
            AccountTab.LoadFromSettings(m_model);
            CacheConfigTab.LoadFromSettings(AccountTab._Model, m_model, AccountTab._Model.SqlConnection, AccountTab.CatalogID);
        }
    }

    private void DoSave(object sender, RoutedEventArgs e)
    {
        // launcher will interrogate us and save the settings if we return true.
        DialogResult = true;
    }

    private void CreateNewProfile(object sender, RoutedEventArgs e)
    {
        if (InputBox.FPrompt("New profile name", "", out string? newProfileName, this))
        {
            Profile newProfile = new Profile();
            newProfile.Name = newProfileName;
            ProfileOptions options = new ProfileOptions(newProfile);

            m_model.ProfileOptions.Add(options);
            m_model.CurrentProfile = options;
        }
    }

    private void CreateNewBasedOnProfile(object sender, RoutedEventArgs e)
    {
        if (InputBox.FPrompt("New profile name", "", out string? newProfileName, this))
        {
            Profile newProfile = new Profile(m_model.CurrentProfile?.Profile ?? new Profile());

            newProfile.Name = newProfileName;
            ProfileOptions options = new ProfileOptions(newProfile);

            m_model.ProfileOptions.Add(options);
            m_model.CurrentProfile = options;
        }
    }
}