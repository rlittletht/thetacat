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
using System.Windows.Forms.VisualStyles;
using MetadataExtractor;
using MetadataExtractor.Formats.Xmp;
using Thetacat.Logging;
using Thetacat.Migration.Elements.Metadata.UI;
using Thetacat.Model;
using Thetacat.Model.Metatags;
using Thetacat.Standards;
using Thetacat.Types;
using Thetacat.Util;

namespace Thetacat.Migration.Elements.Media;

public class PseMediaItem : INotifyPropertyChanged, IPseMediaItem, IMediaItemFile, ICheckableListViewItem
{
    private TriState m_pathVerified;
    private Dictionary<Guid, string>? m_metadataValues;
    private Dictionary<string, string>? m_pseMetadataValues;
    private List<PseMetatag>? m_pseMetatagValues;

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
    public PathSegment? VerifiedPath { get; set; }

    public bool InCatalog
    {
        get => m_inCatalog;
        set
        {
            if (value == m_inCatalog) return;
            m_inCatalog = value;
            OnPropertyChanged();
        }
    }

    public string? MD5
    {
        get => m_md5;
        set
        {
            if (value == m_md5) return;
            m_md5 = value;
            OnPropertyChanged();
        }
    }

    public bool Checked
    {
        get => m_include;
        set => SetField(ref m_include, value);
    }

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

    public IEnumerable<PseMetatag> Tags => m_pseMetatagValues ??= new();
    public List<PseMetatag> PseMetatags => m_pseMetatagValues ??= new();

    // this maps PseIdentifier to the value. The PseIdentifier is the identifier
    // from the metadata_description_table (not the integer id)
    public Dictionary<string, string> PseMetadataValues
    {
        get => m_pseMetadataValues ??= new();
        set => SetField(ref m_pseMetadataValues, value);
    }

    public Dictionary<Guid, string> MetadataValues
    {
        get => m_metadataValues ??= new();
        set => SetField(ref m_metadataValues, value);
    }

    public TriState PathVerified
    {
        get => m_pathVerified;
        set => SetField(ref m_pathVerified, value);
    }

    public PseMediaItem()
    {
        PathVerified = TriState.Maybe;
    }

    private List<PseMediaTagValue>? m_mediaTagValues;
    private bool m_include;
    private bool m_inCatalog;
    private string? m_md5;

    public IEnumerable<PseMediaTagValue> Metadata => m_mediaTagValues ??= BuildTagValues();

    private List<PseMediaTagValue> BuildTagValues()
    {
        List<PseMediaTagValue> tags = new();
        foreach (string identifier in PseMetadataValues.Keys)
        {
            string value = PseMetadataValues[identifier];

            tags.Add(
                new PseMediaTagValue()
                {
                    MediaId = ID,
                    Value = value,
                    PseIdentifier = identifier
                });
        }

        return tags;
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
            if (parent == null)
                appState.MetatagSchema.AddStandardRoot(dirTag);
            else
                appState.MetatagSchema.AddMetatag(dirTag);
        }

        foreach (Tag tag in directory.Tags)
        {
            Metatag? metatag = appState.MetatagSchema.FindByName(dirTag, tag.Name);

            if (metatag == null)
            {
                // need to create a new one
                metatag = Metatag.Create(dirTag?.ID, tag.Name, tag.Name, standard);
                appState.MetatagSchema.AddMetatag(metatag);
            }

            // Description is the value
            MetadataValues.Add(metatag.ID, tag.Description ?? string.Empty);
        }
    }

    string GetFullyQualifiedForSlashed()
    {
        return VerifiedPath?.ToString() ?? $"{VolumeName}/{FullPath}";
    }

    public string FullyQualifiedPath => VerifiedPath?.Local ?? new PathSegment(GetFullyQualifiedForSlashed()).Local;

    public void UpdateCatalogStatus(bool verifyMd5)
    {
        if (PathVerified != TriState.Yes)
            return;

        if (InCatalog)
            return;

        MediaItem? item = App.State.Catalog.LookupItemFromVirtualPath(FullPath, VerifiedPath!, verifyMd5);

        if (item != null)
        {
            InCatalog = true;
            MD5 = item.MD5;
            CatID = item.ID;
        }
    }

    public void CheckPath(Dictionary<string, string> subst, bool verifyMd5)
    {
        if (PathVerified == TriState.Yes)
            return;

        string newPath = GetFullyQualifiedForSlashed();

        foreach (string key in subst.Keys)
        {
            newPath = newPath.Replace(key, subst[key]);
        }

        newPath = newPath.Replace("/", "\\");

        PathVerified = Path.Exists(newPath) ? TriState.Yes : TriState.No;

        if (PathVerified == TriState.Yes)
            VerifiedPath = new PathSegment(newPath);

        if (PathVerified == TriState.Yes)
        {
            // see if we think we already have this item in our catalog
            UpdateCatalogStatus(verifyMd5);
        }

        // MainWindow.LogForAsync(EventType.Information, $"verified path for {GetFullyQualifiedForSlashed()}=>{newPath}: {PathVerified}. InCatalog: {InCatalog}");
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
