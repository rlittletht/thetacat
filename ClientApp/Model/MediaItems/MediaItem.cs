using MetadataExtractor;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using HeyRed.Mime;
using MetadataExtractor.Formats.Exif;
using Thetacat.Filtering;
using Thetacat.Import;
using Thetacat.Logging;
using Thetacat.ServiceClient;
using Thetacat.Standards;
using Thetacat.Types;
using Thetacat.Util;
using Thetacat.Metatags.Model;
using Thetacat.Model.ImageCaching;
using System.Diagnostics.CodeAnalysis;

namespace Thetacat.Model;

public class MediaItem : INotifyPropertyChanged
{
    public enum Op
    {
        Create,
        Delete,
        MaybeUpdate
    };

    public event EventHandler<DirtyItemEventArgs<Guid>>? OnItemDirtied;

    private MediaItemData? m_base;
    private readonly MediaItemData m_working;
    private bool m_isCachePending = false;

    public MediaItem()
    {
        m_working = new MediaItemData();
    }

    public MediaItem(ImportItem importItem)
    {
        m_working = new MediaItemData(importItem);
    }

    public MediaItem(MediaItem clone)
    {
        m_working = new MediaItemData(clone.m_working);
        Stacks[MediaStackType.Version] = clone.VersionStack;
        Stacks[MediaStackType.Media] = clone.MediaStack;
    }

    public MediaItem(ServiceMediaItem item)
    {
        m_working = new MediaItemData(item);
    }

    public static MediaItem CreateNewBasedOn(MediaItem based)
    {
        MediaItem item = new MediaItem();

        item.Data.State = MediaItemState.Pending;
        item.Data.ID = Guid.NewGuid();
        item.Data.MD5 = based.MD5;
        item.Data.MimeType = based.MimeType;
        item.Data.VirtualPath = based.VirtualPath;

        // now clone the metatags
        foreach (KeyValuePair<Guid, MediaTag> kvpTag in based.Tags)
        {
            item.FAddOrUpdateMediaTag(new MediaTag(kvpTag.Value.Metatag, kvpTag.Value.Value), false);
        }

        return item;
    }

    public void TriggerItemDirtied()
    {
        if (OnItemDirtied != null)
            OnItemDirtied(this, new DirtyItemEventArgs<Guid>(ID));
    }

#region Public Data / Accessors

    // the vector clock changes whenever a change is made to the data. we use this
    // clock to determine if a diffitem (when committed) should clear any changes to
    // this item

    public int VectorClock = 0;

    public MediaItemData Data => m_working;

    private void VerifyMediaInMediaStack(MediaStacks stacks, Guid stackId)
    {
        if (!stacks.Items.TryGetValue(stackId, out MediaStack? stack))
            throw new CatExceptionInternalFailure(
                $"can't set the version stack of an item without first adding it to the stack: {stackId}. {stacks.Items.Count}: {stacks.Items.Keys.First()}");

        foreach (MediaStackItem item in stack.Items)
        {
            if (item.MediaId == ID)
                return;
        }

        throw new CatExceptionInternalFailure($"can't set the version stack of an item without first adding it to the stack: {ID}");
    }

    // This has to be kept up to date with the types defined in MediaStackType.cs
    public readonly Guid?[] Stacks =
    {
        null,
        null
    };

    public void OnStackCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        TriggerItemDirtied();
    }

    void SetStackForStackType(ICatalog catalog, Guid? stackId, MediaStackType type)
    {
        if (EqualityComparer<Guid?>.Default.Equals(Stacks[type], stackId)) return;

        MediaStacks catalogStacks =
            type == MediaStackType.Version
                ? catalog.VersionStacks
                : catalog.MediaStacks;

        // if there was already a version stack, make sure to remove any registered event
        // handle for the collection change
        if (Stacks[type] != null)
        {
            MediaStack stack = catalogStacks.Items[Stacks[type]!.Value];
            stack.CollectionChanged -= OnStackCollectionChanged;
        }

        // and now register out  event handler for collection changes
        if (stackId != null)
        {
            MediaStack stack = catalogStacks.Items[stackId.Value];
            stack.CollectionChanged += OnStackCollectionChanged;
        }

        Stacks[type] = stackId;
        if (type == MediaStackType.Version)
            OnPropertyChanged(nameof(VersionStack));
        else
            OnPropertyChanged(nameof(MediaStack));

        if (stackId != null)
            VerifyMediaInMediaStack(catalogStacks, stackId.Value);
    }

    public void SetVersionStackVerify(ICatalog catalog, Guid? stackId)
    {
        SetStackForStackType(catalog, stackId, MediaStackType.Version);
    }

    public void SetMediaStackVerify(ICatalog catalog, Guid? stackId)
    {
        SetStackForStackType(catalog, stackId, MediaStackType.Media);
    }

    public Guid? VersionStack
    {
        get => Stacks[MediaStackType.Version];
        set => SetField(ref Stacks[MediaStackType.Version], value);
    }

    public Guid? MediaStack
    {
        get => Stacks[MediaStackType.Media];
        set => SetField(ref Stacks[MediaStackType.Media], value);
    }

    // this means we are waiting for this item to be cached. maybe by this client,
    // maybe by another client
    public bool IsCachePending
    {
        get => m_isCachePending;
        set => SetField(ref m_isCachePending, value);
    }

    public Op PendingOp { get; set; } = Op.MaybeUpdate;

    public string MimeType
    {
        get => m_working.MimeType;
        set
        {
            EnsureBase();
            VectorClock++;
            m_working.MimeType = value;
        }
    }

    public Guid ID => m_working.ID;

    public PathSegment VirtualPath
    {
        get => m_working.VirtualPath;
        set
        {
            EnsureBase();
            VectorClock++;
            m_working.VirtualPath = value;
        }
    }

    // this is the MD5 of the media in azure storage (or if pending, this is the
    // MD5 of what *will be* in azure storage
    public string MD5
    {
        get => m_working.MD5;
        set
        {
            EnsureBase();
            VectorClock++;
            m_working.MD5 = value;
        }
    }

    public MediaItemState State
    {
        get => m_working.State;
        set
        {
            EnsureBase();
            m_working.State = value;
            VectorClock++;
            OnPropertyChanged(nameof(State));
        }
    }

    private ConcurrentDictionary<Guid, MediaTag> Tags
    {
        get => m_working.Tags;
        set
        {
            EnsureBase();
            VectorClock++;
            m_working.Tags = value;
        }
    }

    public IEnumerable<MediaTag> MediaTags => m_working.Tags.Values;

    public bool TryGetMediaTag(Guid guid, [NotNullWhen(true)] out MediaTag? mediaTag)
    {
        if (guid == BuiltinTags.s_VirtualPathID)
        {
            // synthesize a mediatag for virtual path
            mediaTag = new MediaTag(BuiltinTags.s_VirtualPath, VirtualPath);
            return true;
        }

        return Tags.TryGetValue(guid, out mediaTag);
    }

    public bool HasMediaTag(Guid guid)
    {
        // first check to see if its a builtin non-schema tag
        if (guid == BuiltinTags.s_VirtualPathID)
            return true;

        return Tags.ContainsKey(guid);
    }

    public int? ImageWidth
    {
        get
        {
            if (Tags.TryGetValue(BuiltinTags.s_Width.ID, out MediaTag? tag))
                return int.Parse(tag.Value ?? "");

            return null;
        }
        set
        {
            MediaTag tag = new MediaTag(BuiltinTags.s_Width, value.ToString());
            FAddOrUpdateMediaTag(tag, true);
            OnPropertyChanged(nameof(ImageWidth));
        }
    }

    public int? ImageHeight
    {
        get
        {
            if (Tags.TryGetValue(BuiltinTags.s_Height.ID, out MediaTag? tag))
                return int.Parse(tag.Value ?? "");

            return null;
        }
        set
        {
            MediaTag tag = new MediaTag(BuiltinTags.s_Height, value.ToString());
            FAddOrUpdateMediaTag(tag, true);
            OnPropertyChanged(nameof(ImageHeight));
        }
    }

    public DateTime? OriginalMediaDate
    {
        get
        {
            if (Tags.TryGetValue(BuiltinTags.s_OriginalMediaDateID, out MediaTag? tag))
                return tag.Value == null ? null : DateTime.Parse(tag.Value);

            return null;
        }
        set
        {
            MediaTag tag = new MediaTag(BuiltinTags.s_OriginalMediaDate, value?.ToUniversalTime().ToString("u"));
            FAddOrUpdateMediaTag(tag, true);
            OnPropertyChanged(nameof(OriginalMediaDate));
        }
    }

    public DateTime? ImportDate
    {
        get
        {
            if (Tags.TryGetValue(BuiltinTags.s_ImportDateID, out MediaTag? tag))
                return tag.Value == null ? null : DateTime.Parse(tag.Value);

            return null;
        }
        set
        {
            MediaTag tag = new MediaTag(BuiltinTags.s_ImportDate, value?.ToUniversalTime().ToString("u"));
            FAddOrUpdateMediaTag(tag, true);
            OnPropertyChanged(nameof(ImportDate));
        }
    }

    private T? GetBuiltinTagValue<T>(Metatag metatag, Func<string, T> parser)
        where T : struct // restrict this to value types (int, string, bool)
    {
        if (Tags.TryGetValue(metatag.ID, out MediaTag? tag))
            return tag.Value == null ? null : parser(tag.Value);

        return null;
    }

    private bool FHasBuiltinTag(Metatag metatag)
    {
        if (Tags.TryGetValue(metatag.ID, out MediaTag? tag))
            return true;

        return false;
    }

    private void SetBuiltinTagToggleTag(Metatag metatag, bool fSet, [CallerMemberName] string? propertyName = null)
    {
        if (fSet)
        {
            MediaTag tag = new MediaTag(metatag, null);
            FAddOrUpdateMediaTag(tag, true);
        }
        else
        {
            FRemoveMediaTag(metatag.ID);
        }

        OnPropertyChanged(propertyName);
    }

    private void SetBuiltinTagValue<T>(Metatag metatag, T? value, [CallerMemberName] string? propertyName = null)
        where T : struct
    {
        if (value != null)
        {
            MediaTag tag = new MediaTag(metatag, value?.ToString());
            FAddOrUpdateMediaTag(tag, true);
        }
        else
        {
            FRemoveMediaTag(metatag.ID);
        }

        OnPropertyChanged(propertyName);
    }

    public int? TransformRotate
    {
        get => GetBuiltinTagValue(BuiltinTags.s_TransformRotate, int.Parse);
        set => SetBuiltinTagValue(BuiltinTags.s_TransformRotate, value);
    }

    public bool IsTrashItem
    {
        get => FHasBuiltinTag(BuiltinTags.s_IsTrashItem);
        set => SetBuiltinTagToggleTag(BuiltinTags.s_IsTrashItem, value);
    }

    public bool DontPushToCloud
    {
        get => FHasBuiltinTag(BuiltinTags.s_DontPushToCloud);
        set => SetBuiltinTagToggleTag(BuiltinTags.s_DontPushToCloud, value);
    }

    public bool TransformMirror
    {
        get => FHasBuiltinTag(BuiltinTags.s_TransformMirror);
        set => SetBuiltinTagToggleTag(BuiltinTags.s_TransformMirror, value);
    }

#endregion

#region Changes/Versions

    public bool MaybeHasChanges => m_base != null;

    void EnsureBase()
    {
        m_base ??= new(m_working);
    }

    public MediaItemData Base => m_base ?? throw new CatExceptionInternalFailure("no base to fetch");

    public void ResetPendingChanges()
    {
        PendingOp = Op.MaybeUpdate;
        m_base = null;
    }

    public bool Equals(MediaItemData other)
    {
        if (other.State != State)
            return false;
        if (other.ID != ID)
            return false;
        if (other.VirtualPath.Equals(VirtualPath))
            return false;
        if (other.MD5 != MD5)
            return false;
        if (other.MimeType != MimeType)
            return false;

        foreach (MediaTag tag in Tags.Values)
        {
            if (!other.Tags.TryGetValue(tag.Metatag.ID, out MediaTag? otherTag))
                return false;

            if (!tag.Equals(otherTag))
                return false;
        }

        return true;
    }

    public void SetPendingStateFromOther(MediaItem? other)
    {
        m_base = other?.Data;

        if (m_base == null)
            PendingOp = Op.Create;
        else
            PendingOp = Op.MaybeUpdate;
    }

#endregion

#region Cache Status/Media State

    public string CacheStatus => App.State.Cache.IsItemCached(ID) ? "cached" : "<No Cache>";

    public void NotifyCacheStatusChanged()
    {
        OnPropertyChanged(nameof(CacheStatus));
    }

    public static string StringFromState(MediaItemState state)
    {
        switch (state)
        {
            case MediaItemState.Active:
                return "active";
            case MediaItemState.Pending:
                return "pending";
        }

        return "unknown";
    }

    public static MediaItemState StateFromString(string state)
    {
        switch (state.ToUpper())
        {
            case "ACTIVE":
                return MediaItemState.Active;
            case "PENDING":
                return MediaItemState.Pending;
        }

        return MediaItemState.Unknown;
    }

#endregion

#region MediaTags

    private static Dictionary<MetatagStandards.Standard, HashSet<int>> PropertiesAllowedToOverride =
        new()
        {
            {
                MetatagStandards.Standard.Exif,
                new HashSet<int>()
                {
                    ExifDirectoryBase.TagImageWidth,
                    ExifDirectoryBase.TagImageHeight,
                    ExifDirectoryBase.TagXResolution,
                    ExifDirectoryBase.TagYResolution
                }
            }
        };

    /*----------------------------------------------------------------------------
        %%Function: PopulateMediaTagsFromMetadataDirectory
        %%Qualified: Thetacat.Model.MediaItem.PopulateMediaTagsFromMetadataDirectory

        Returns true if there were any changes made to this items tags.
        (schema changes will be caught later)
    ----------------------------------------------------------------------------*/
    public bool PopulateMediaTagsFromMetadataDirectory(
        MetatagSchema metatagSchema,
        Metatag? parent,
        MetadataExtractor.Directory directory,
        MetatagStandards.Standard standard,
        bool allowSubifdOverrideIfd,
        List<string>? log = null)
    {
        bool changed = false;

        if (parent == null && standard == MetatagStandards.Standard.Unknown)
            standard = MetatagStandards.GetStandardFromType(directory.GetType().Name);

        if (standard == MetatagStandards.Standard.Unknown)
        {
#if verbose_standard
            MainWindow.LogForApp(EventType.Verbose, $"unknown standard directory {directory.Name}");
            log?.Add($"unknown standard directory {directory.Name}");
#endif
            return false;
        }

        StandardDefinitions standardDefinitions = MetatagStandards.GetStandardMappings(standard);

        // match the current directory to a metatag
        Metatag dirTag = metatagSchema.GetOrBuildDirectoryTag(parent, standard, directory.Name);

        foreach (Tag tag in directory.Tags)
        {
            if (!standardDefinitions.Properties.TryGetValue(tag.Type, out StandardDefinition? def))
            {
#if verbose_standard
                MainWindow.LogForApp(EventType.Verbose, $"unknown metatag {tag.Name} in standard {standardDefinitions.StandardId}");
                log?.Add($"unknown metatag {tag.Name} in standard {standardDefinitions.StandardId}");
#endif
                continue;
            }

            if (!def.Include)
                continue;
            Metatag? metatag = metatagSchema.FindByName(dirTag, def.PropertyTagName);

            string value = tag.Description ?? string.Empty;

            if (string.IsNullOrEmpty(value) || value == " " || value == "  ")
                continue;

            value = value.Replace(" pixels", "");

            if (metatag == null)
            {
                // need to create a new one
                metatag = Metatag.Create(dirTag?.ID, def.PropertyTagName, tag.Name, standard);
                metatagSchema.AddMetatag(metatag);
            }

            MediaTag newTag = new MediaTag(metatag, value);

            bool allowPropertyOverride = false;

            if (allowSubifdOverrideIfd)
            {
                if (PropertiesAllowedToOverride.TryGetValue(standard, out HashSet<int>? set))
                    allowPropertyOverride = set.Contains(def.PropertyTag);
            }

            if (FAddOrUpdateMediaTag(newTag, allowPropertyOverride, log))
                changed = true;
        }

        return changed;
    }

    public bool FRemoveMediaTag(Guid mediaTagID)
    {
        bool ensuredBase = m_base == null;

        EnsureBase();

        if (!Tags.TryRemove(mediaTagID, out MediaTag? value))
        {
            if (ensuredBase)
                ResetPendingChanges();

            return false;
        }

        OnPropertyChanged(nameof(Tags));
        TriggerItemDirtied();
        VectorClock++;
        return true;
    }

    /*----------------------------------------------------------------------------
        %%Function: AddOrUpdateMediaTagInternal
        %%Qualified: Thetacat.Model.MediaItem.AddOrUpdateMediaTagInternal

       This circumvents the normal dirtying of the item -- DO NOT use this
       directly unless you know what you are really doing (e.g. you are reading
       from the database which means its by definition not a dirtying action)
    ----------------------------------------------------------------------------*/
    public void AddOrUpdateMediaTagInternal(MediaTag tag)
    {
        Tags.AddOrUpdate(
            tag.Metatag.ID,
            tag,
            (key, oldTag) =>
            {
                oldTag.Value = tag.Value;
                return oldTag;
            });
    }

    /*----------------------------------------------------------------------------
        %%Function: FAddOrUpdateMediaTag
        %%Qualified: Thetacat.Model.MediaItem.FAddOrUpdateMediaTag

        Add or updated a media tag
    ----------------------------------------------------------------------------*/
    public bool FAddOrUpdateMediaTag(MediaTag tag, bool allowPropertyOverride, List<string>? log = null)
    {
        bool ensuredBase = m_base == null;

        EnsureBase();

        bool identicalExisting = false;

        Tags.AddOrUpdate(
            tag.Metatag.ID,
            tag,
            (key, oldMediaTag) =>
            {
                if (oldMediaTag.Value != tag.Value)
                {
                    if (!allowPropertyOverride)
                    {
                        App.LogForApp(EventType.Verbose, $"Different metatag value for {key}: {oldMediaTag.Value} != {tag.Value}");
                        log?.Add($"Different metatag value for {key}: {oldMediaTag.Value} != {tag.Value}");
                    }

                    oldMediaTag.Value = tag.Value;
                }
                else
                {
                    identicalExisting = true;
                }

                return oldMediaTag;
            });

        if (identicalExisting && ensuredBase)
        {
            // well drat, we ended up not needing to ensure the base (because the tag was
            // identical). free the base so we don't think there's a difference
            ResetPendingChanges();
        }

        if (!identicalExisting)
            VectorClock++;

        if (!identicalExisting)
            OnPropertyChanged(nameof(Tags));

        return !identicalExisting;
    }


    Tuple<int, int>? TryGetWidthFromStandardAndType(MetatagSchema schema, MetatagStandards.Standard standard, int widthType, int heightType)
    {
        Metatag? tagWidth = schema.FindStandardItemFromStandardAndType(standard, widthType);
        if (tagWidth == null)
            return null;

        Metatag? tagHeight = schema.FindStandardItemFromStandardAndType(standard, heightType);

        if (tagHeight == null)
            return null;

        if (!Tags.TryGetValue(tagWidth.ID, out MediaTag? matchWidth))
            return null;

        if (!Tags.TryGetValue(tagHeight.ID, out MediaTag? matchHeight))
            return null;

        return new Tuple<int, int>(int.Parse(matchWidth.Value ?? ""), int.Parse(matchHeight.Value ?? ""));
    }

    string? TryGetMediaTagValueFromStandardAndType(MetatagSchema schema, MetatagStandards.Standard standard, int type)
    {
        Metatag? tag = schema.FindStandardItemFromStandardAndType(standard, type);
        if (tag == null)
            return null;

        if (!Tags.TryGetValue(tag.ID, out MediaTag? match))
            return null;

        return match.Value;
    }

    /*----------------------------------------------------------------------------
        %%Function: FindWidthHeightFromMediaTags
        %%Qualified: Thetacat.Model.MediaItem.FindWidthHeightFromMediaTags

        There are a lot of different mediatag items for width. Find at least one
        of them so we can promote to our builtin width/height
    ----------------------------------------------------------------------------*/
    Tuple<int, int>? FindWidthHeightFromMediaTags(MetatagSchema schema)
    {
        Tuple<int, int>? widthHeight;

        widthHeight = TryGetWidthFromStandardAndType(
            schema,
            MetatagStandards.Standard.Jpeg,
            MetadataExtractor.Formats.Jpeg.JpegDirectory.TagImageWidth,
            MetadataExtractor.Formats.Jpeg.JpegDirectory.TagImageHeight);

        if (widthHeight != null)
            return widthHeight;

        widthHeight = TryGetWidthFromStandardAndType(
            schema,
            MetatagStandards.Standard.Exif,
            MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagImageWidth,
            MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagImageHeight);

        if (widthHeight != null)
            return widthHeight;

        return null;
    }

    /*----------------------------------------------------------------------------
        %%Function: SetMediaTagsFromFileMetadata
        %%Qualified: Thetacat.Model.MediaItem.SetMediaTagsFromFileMetadata

        Parse the file for this media item and extract all the mediatags
        (that we have mappings for)
    ----------------------------------------------------------------------------*/
    public List<string>? SetMediaTagsFromFileMetadata(MetatagSchema metatagSchema, string localFilePath)
    {
        List<string> log = new List<string>();

        string file = localFilePath;
        bool allowSubifdOverrideIfd = file.ToLowerInvariant().EndsWith(".nef");

        // load exif and other data from this item.

//        if (MimeType == MimeTypesMap.GetMimeType("test.jp2"))
//        {
//            // this is a jpeg2000 file. have to use emgu
//            Mat mat = CvInvoke.Imread(file, ImreadModes.AnyColor);
//            Image<Bgr, Byte> img = mat.ToImage<Bgr, Byte>();
//
//            MessageBox.Show($"num: {img.")
//        }
//        else
        {
            IEnumerable<MetadataExtractor.Directory> directories = ImageMetadataReader.ReadMetadata(file);

            this.MD5 = CalculateMD5Hash(file);

            foreach (MetadataExtractor.Directory directory in directories)
            {
                PopulateMediaTagsFromMetadataDirectory(metatagSchema, null, directory, MetatagStandards.Standard.Unknown, allowSubifdOverrideIfd, log);
            }
        }

        metatagSchema.EnsureBuiltinMetatagsDefined();

        // lastly, set the builtin items
        Tuple<int, int>? widthHeight = FindWidthHeightFromMediaTags(metatagSchema);
        if (widthHeight != null)
        {
            FAddOrUpdateMediaTag(new MediaTag(BuiltinTags.s_Width, widthHeight.Item1.ToString()), true);
            FAddOrUpdateMediaTag(new MediaTag(BuiltinTags.s_Height, widthHeight.Item2.ToString()), true);
        }

        FAddOrUpdateMediaTag(new MediaTag(BuiltinTags.s_ImportDate, DateTime.Now.ToUniversalTime().ToString("u")), true);

        string? dateTimeString =
            TryGetMediaTagValueFromStandardAndType(
                metatagSchema,
                MetatagStandards.Standard.Exif,
                ExifDirectoryBase.TagDateTimeOriginal);

        if (dateTimeString != null)
        {
            try
            {
                // try to get it into a canonical format
                if (dateTimeString.Length >= 19 && dateTimeString[4] == ':' && dateTimeString[7] == ':')
                {
                    // replace the : in the data with -
                    dateTimeString = $"{dateTimeString[..4]}-{dateTimeString[5..7]}-{dateTimeString[8..10]}{dateTimeString[10..]}";
                    dateTimeString = DateTime.Parse(dateTimeString).ToUniversalTime().ToString("u");
                }
            }
            catch
            {
                App.LogForApp(EventType.Warning, $"Could not get date from metadata for {file}");
                dateTimeString = null;
            }
        }

        dateTimeString ??= File.GetCreationTime(file).ToUniversalTime().ToString("u");

        FAddOrUpdateMediaTag(new MediaTag(BuiltinTags.s_OriginalMediaDate, dateTimeString), true);

        return log;
    }

#endregion

#region Stacks

#endregion

#region INotifyPropertyChanged

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        TriggerItemDirtied();
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        TriggerItemDirtied();
        OnPropertyChanged(propertyName);
        return true;
    }

#endregion

    /*----------------------------------------------------------------------------
        %%Function: CalculateMD5Hash
        %%Qualified: Thetacat.Model.MediaItem.CalculateMD5Hash

        Calculate the MD5 hash for the given file.
    ----------------------------------------------------------------------------*/
    public static string CalculateMD5Hash(string localPath)
    {
        return App.State.Md5Cache.GetMd5ForPathSync(localPath);

//        using FileStream fs = File.Open(
//            localPath,
//            FileMode.Open,
//            FileAccess.Read,
//            FileShare.Read);
//
//        using MD5 md5 = System.Security.Cryptography.MD5.Create();
//
//        byte[] hash = md5.ComputeHash(fs);
//
//        string fullContentMd5 = Convert.ToBase64String(hash);
//
//        return fullContentMd5;
    }

    /*----------------------------------------------------------------------------
        %%Function: MatchesMetatagFilter
        %%Qualified: Thetacat.Model.MediaItem.MatchesMetatagFilter

        The filter defines the tags that must not be set (false) or must be set
        (true). If its not present then it doesn't matter
    ----------------------------------------------------------------------------*/
    public bool MatchesMetatagFilter(FilterDefinition filter)
    {
        return filter.Expression.FEvaluate(new FilterValueClient(this));
    }
}
