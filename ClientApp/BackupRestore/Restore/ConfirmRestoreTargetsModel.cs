using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Thetacat.BackupRestore.Restore
{
    public class ConfirmRestoreTargetsModel : INotifyPropertyChanged
    {
        private ObservableCollection<string> m_profiles = new();
        private string m_referenceSqlConnection = string.Empty;
        private string m_referenceWorkgroupName = string.Empty;
        private string m_referenceWorkgroupId = string.Empty;
        private string m_referenceCatalogId = string.Empty;
        private string m_referenceProfile = string.Empty;

        private string m_targetSqlConnection = string.Empty;
        private string m_targetWorkgroupId = string.Empty;
        private string m_targetWorkgroupName = string.Empty;
        private string m_targetCatalogId = string.Empty;
        private string m_targetProfile = string.Empty;
        private string m_guidMapExportPath = string.Empty;

        public string GuidMapExportPath
        {
            get => m_guidMapExportPath;
            set => SetField(ref m_guidMapExportPath, value);
        }

        public ObservableCollection<string> Profiles
        {
            get => m_profiles;
            set => SetField(ref m_profiles, value);
        }

        public string ReferenceProfile
        {
            get => m_referenceProfile;
            set => SetField(ref m_referenceProfile, value);
        }

        public string TargetProfile
        {
            get => m_targetProfile;
            set => SetField(ref m_targetProfile, value);
        }

        public string TargetCatalogID
        {
            get => m_targetCatalogId;
            set => SetField(ref m_targetCatalogId, value);
        }

        public string ReferenceCatalogID
        {
            get => m_referenceCatalogId;
            set
            {
                if (value == m_referenceCatalogId) return;
                m_referenceCatalogId = value;
                OnPropertyChanged();
            }
        }

        public string ReferenceSqlConnection
        {
            get => m_referenceSqlConnection;
            set => SetField(ref m_referenceSqlConnection, value);
        }

        public string ReferenceWorkgroupName
        {
            get => m_referenceWorkgroupName;
            set => SetField(ref m_referenceWorkgroupName, value);
        }

        public string ReferenceWorkgroupId
        {
            get => m_referenceWorkgroupId;
            set => SetField(ref m_referenceWorkgroupId, value);
        }

        public string TargetSqlConnection
        {
            get => m_targetSqlConnection;
            set => SetField(ref m_targetSqlConnection, value);
        }

        public string TargetWorkgroupId
        {
            get => m_targetWorkgroupId;
            set => SetField(ref m_targetWorkgroupId, value);
        }

        public string TargetWorkgroupName
        {
            get => m_targetWorkgroupName;
            set => SetField(ref m_targetWorkgroupName, value);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
