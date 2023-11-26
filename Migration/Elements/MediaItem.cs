using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using Thetacat.Types;

namespace Thetacat.Migration.Elements;

public class MediaItem : INotifyPropertyChanged
{
    private TriState m_pathVerified;
    public string ID { get; set; } = String.Empty;

    public string Filename { get; set; } = String.Empty;
    public string FullPath { get; set; } = String.Empty;
    public string FilePathSearch { get; set; } = String.Empty;
    public string MimeType { get; set; } = String.Empty;
    public string VolumeId { get; set; } = String.Empty;
    public string VolumeName { get; set; } = String.Empty;

    public TriState PathVerified
    {
        get => m_pathVerified;
        set
        {
            if (value == m_pathVerified) return;
            m_pathVerified = value;
            OnPropertyChanged();
        }
    }

    public MediaItem()
    {
        PathVerified = TriState.Maybe;
    }

    public void CheckPath(Dictionary<string, string> subst)
    {
        if (PathVerified == TriState.Yes)
            return;

        string newPath = FullPath;

        foreach (string key in subst.Keys)
        {
            newPath = newPath.Replace(key, subst[key]);
        }

        newPath = newPath.Replace("/", "\\");

        PathVerified = Path.Exists(newPath) ? TriState.Yes : TriState.No;
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