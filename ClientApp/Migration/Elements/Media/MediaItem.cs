using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using MetadataExtractor;
using MetadataExtractor.Formats.Xmp;
using Thetacat.Model;
using Thetacat.Standards;
using Thetacat.Types;

namespace Thetacat.Migration.Elements.Media;

public class MediaItem : INotifyPropertyChanged, IMediaItem
{
    private TriState m_pathVerified;
    private Dictionary<Guid, string>? m_metatagValues;
    private Dictionary<int, string>? m_pseMetatagValues;
    private Guid m_catId;
    private int m_id;

    public string Filename { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public string FilePathSearch { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public string VolumeId { get; set; } = string.Empty;
    public string VolumeName { get; set; } = string.Empty;
    public int ImageWidth { get; set; }
    public int ImageHeight { get; set; }
    public DateTime FileDateOriginal { get; set; }

    public int ID
    {
        get => m_id;
        set => SetField(ref m_id, value);
    }

    public Guid CatID
    {
        get => m_catId;
        set => SetField(ref m_catId, value);
    }

    public Dictionary<int, string> PseMetatagValues
    {
        get => m_pseMetatagValues ??= new();
        set => SetField(ref m_pseMetatagValues, value);
    }

    public Dictionary<Guid, string> MetatagValues
    {
        get => m_metatagValues ??= new();
        set => SetField(ref m_metatagValues, value);
    }

    public TriState PathVerified
    {
        get => m_pathVerified;
        set => SetField(ref m_pathVerified, value);
    }

    public MediaItem()
    {
        PathVerified = TriState.Maybe;
    }

    public void MigrateMetadataForDirectory(IAppState appState, Metatag? parent, MetadataExtractor.Directory directory, MetatagStandards.Standard standard)
    {
        if (parent == null && standard == MetatagStandards.Standard.Unknown)
            standard = MetatagStandards.GetStandardFromType(directory.GetType().Name);

        // match the current directory to a metatag

        //        MetadataExtractor.Formats.Jpeg.JpegDirectory.TagImageHeight
        Debug.Assert(appState.MetatagSchema != null, "appState.MetatagSchema != null");
        Metatag? dirTag = appState.MetatagSchema.FindByName(parent, directory.Name);

        if (dirTag == null)
        {
            // we have to create one
            dirTag = Metatag.Create(parent?.ID, directory.Name, directory.Name, standard);
            appState.MetatagSchema.AddMetatag(dirTag);
        }

        foreach (Tag tag in directory.Tags)
        {
            Metatag? metatag = appState.MetatagSchema.FindByName(dirTag, tag.Name);

            if (metatag == null)
            {
                // need to create a new one
                metatag = Metatag.Create(dirTag?.ID, tag.Name, tag.Name, standard);
            }

            // Description is the value
            MetatagValues.Add(metatag.ID, tag.Description ?? string.Empty);
        }
    }

    public void CheckPath(IAppState appState, Dictionary<string, string> subst)
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

        if (PathVerified == TriState.Yes)
        {
            // load exif and other data from this item.
            IEnumerable<MetadataExtractor.Directory> directories = ImageMetadataReader.ReadMetadata(newPath);

            foreach (MetadataExtractor.Directory directory in directories)
            {
                MigrateMetadataForDirectory(appState, null, directory, MetatagStandards.Standard.Unknown);
            }
        }
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
