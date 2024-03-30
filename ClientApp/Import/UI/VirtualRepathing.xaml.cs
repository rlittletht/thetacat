using Emgu.CV.Dnn;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Thetacat.Import.UI.Commands;
using Thetacat.Model;
using Thetacat.Types;
using Thetacat.UI.Input;
using Thetacat.Util;

namespace Thetacat.Import.UI;

/// <summary>
/// Interaction logic for VirtualRepathing.xaml
/// </summary>
public partial class VirtualRepathing : Window
{
    readonly VirtualRepathingModel m_model = new VirtualRepathingModel();

    public VirtualRepathingModel Model => m_model;
    public VirtualRepathing()
    {
        m_model.AddPathToRepathMapCommand = new AddPathToRepathMapCommand(_AddPathToRepathMapCommand);
        m_model.RemoveMappingCommand = new RemoveMappingCommand(_RemoveMappingCommand);
        DataContext = m_model;
        InitializeComponent();
        InitializeVirtualRoots();
        LoadMapStore();

        m_model.PropertyChanged += ModelOnPropertyChanged;
    }

    string MakeToFromPattern(string from, string to, string newFrom)
    {
        if (to.Contains(from))
        {
            // get the prefix and suffix
            int start = to.IndexOf(from, StringComparison.Ordinal);

            return to.Substring(0, start) + newFrom + to.Substring(start + from.Length);
        }

        return newFrom;
    }

    void _RemoveMappingCommand(RepathItem? item)
    {
        if (item != null)
        {
            m_model.RepathItems.Remove(item);
        }
    }

    void _AddPathToRepathMapCommand(IBackingTreeItem? context)
    {
        if (context is BackingTreeItem<VirtualRootNameItem> nameItem)
        {
            string from = m_model.MapFrom;
            string to = m_model.MapTo;

            m_model.MapFrom = $"{nameItem.Data.FullName}/";

            // see if the old MapTo is a transformation
            m_model.MapTo = MakeToFromPattern(from, to, m_model.MapFrom);
        }
    }

    private void ModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == "CurrentMap")
        {
            m_model.RepathItems.Clear();

            foreach (MapStore.MapPair pair in m_model.MapStore!.Mappings[m_model.CurrentMap])
            {
                m_model.RepathItems.Add(new RepathItem(new PathSegment(pair.From), new PathSegment(pair.To), RepathItemType.Repath));
            }
        }
        
    }

    void LoadMapStore()
    {
        m_model.MapStore = new MapStore();

        foreach (KeyValuePair<string, List<MapStore.MapPair>> item in m_model.MapStore.Mappings)
        {
            m_model.Maps.Add(item.Key);
        }
    }

    void SaveMapStore()
    {
        m_model.MapStore?.WriteStore();
    }

    private int VirtualRootNameItemComparer(VirtualRootNameItem x, VirtualRootNameItem y)
    {
        return string.Compare(x.FullName, y.FullName, StringComparison.CurrentCultureIgnoreCase);
    }

    static KeyValuePair<string?, VirtualRootNameItem>[] VirtualRootSplitter(VirtualRootNameItem data)
    {
        // simple string splitter
        string[] split = data.FullName.Split("/");
        List<KeyValuePair<string?, VirtualRootNameItem>> newData = new();

        int i = 0;
        PathSegment segment = PathSegment.Empty;
        for (; i < split.Length - 1; i++)
        {
            string s = split[i];
            segment = PathSegment.Join(segment, s);
            newData.Add(new KeyValuePair<string?, VirtualRootNameItem>(s, new VirtualRootNameItem(s, segment)));
        }

        if (i == split.Length - 1)
            newData.Add(new KeyValuePair<string?, VirtualRootNameItem>(null, new VirtualRootNameItem(split[i], data.FullName)));

        return newData.ToArray();
    }

    string RepathVirtualPath(string repathed, bool fragment, IReadOnlyCollection<RepathItem> repathItems)
    {
        if (fragment)
            repathed = $"{repathed}/";

        foreach (RepathItem repathItem in repathItems)
        {
            repathed = repathed.Replace(repathItem.From.ToString(), repathItem.To.ToString());
        }

        if (fragment)
        {
            if (repathed.EndsWith('/'))
                repathed = repathed.Substring(0, repathed.Length - 1);
            else
                MessageBox.Show($"repathed doesn't end with '/': {repathed}");
        }

        return repathed;
    }

    PathSegment RepathVirtualPath(PathSegment segment, IReadOnlyCollection<RepathItem> repathItems)
    {
        return new PathSegment(RepathVirtualPath(segment.ToString(), false /*fragment*/, repathItems));
    }

    BackingTree CreatePreviewTree(IReadOnlyCollection<RepathItem> repathItems)
    {
        HashSet<string> roots = new();

        HashSet<string> check = new();
        foreach (RepathItem item in repathItems)
        {
            if (check.Contains(item.From))
            {
                MessageBox.Show($"Warning:  ${item.From} shows up more than once as a source!");
            }

            check.Add(item.From);
        }

        foreach (VirtualRootNameItem item in m_model.OriginalRoots)
        {
            string repathed = RepathVirtualPath(item.FullName, true, repathItems);

            roots.Add(repathed);
        }

        List<VirtualRootNameItem> items = new();

        foreach (string root in roots)
        {
            items.Add(new VirtualRootNameItem(root));
        }

        items.Sort(VirtualRootNameItemComparer);
        return BackingTree.CreateFromList<VirtualRootNameItem, string>(items, VirtualRootSplitter, new VirtualRootNameItem(""));
    }

    BackingTree CreateVirtualBackingTree()
    {
        HashSet<string> roots = new();

        foreach (MediaItem item in App.State.Catalog.GetMediaCollection())
        {
            roots.Add(item.VirtualPath.GetPathDirectory());
        }

        List<VirtualRootNameItem> items = new();

        foreach (string root in roots)
        {
            items.Add(new VirtualRootNameItem(root));
        }

        items.Sort(VirtualRootNameItemComparer);
        m_model.OriginalRoots.AddRange(items);

        return BackingTree.CreateFromList<VirtualRootNameItem, string>(items, VirtualRootSplitter, new VirtualRootNameItem(""));
    }

    void InitializeVirtualRoots()
    {
        OriginalTree.Clear();

        BackingTree tree = CreateVirtualBackingTree();
        OriginalTree.Initialize(tree.Children);
//        m_model.OriginalRoots.AddRange(items);
    }

    private void DoCancel(object sender, RoutedEventArgs e)
    {
        this.Close();
    }

    private void DoRepathing(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show(
                "Do you want to repath the catalog with these mappings? You will still need to commit the catalog changes.",
                "Thetacat",
                MessageBoxButton.OKCancel)
            == MessageBoxResult.OK)
        {
            foreach (MediaItem item in App.State.Catalog.GetMediaCollection())
            {
                PathSegment repathed = RepathVirtualPath(item.VirtualPath, m_model.RepathItems);

                if (!repathed.Equals(item.VirtualPath))
                {
                    item.VirtualPath = repathed;
                    item.TriggerItemDirtied();
                }
            }
        }

        this.Close();
    }

    private void DoAddMapping(object sender, RoutedEventArgs e)
    {
        m_model.RepathItems.Add(new RepathItem(new PathSegment(m_model.MapFrom), new PathSegment(m_model.MapTo), RepathItemType.Repath));
    }

    private void UpdatePreview(object sender, RoutedEventArgs e)
    {
        // apply all the repathings and update the 
        PreviewTree.Clear();
        BackingTree tree = CreatePreviewTree(m_model.RepathItems);
        PreviewTree.Initialize(tree.Children);
    }

   
    private void SaveMappings(object sender, RoutedEventArgs e)
    {
        if (!InputBox.FPrompt("Name for this mapping", "Root Map 1", out string? mapName, this))
            return;

        // check if this already exists in the store
        List<MapStore.MapPair> maps = new List<MapStore.MapPair>();
        foreach (RepathItem item in m_model.RepathItems)
        {
            maps.Add(new MapStore.MapPair(item.From, item.To));
        }

        string stringKey = string.Empty;
        // see if we are updating or adding
        foreach (KeyValuePair<string, List<MapStore.MapPair>> mapsItem in m_model.MapStore!.Mappings)
        {
            if (string.Compare(mapsItem.Key, mapName, StringComparison.CurrentCultureIgnoreCase) == 0)
            {
                stringKey = mapsItem.Key;
                break;
            }
        }

        if (stringKey == string.Empty)
        {
            m_model.Maps.Add(mapName);
            stringKey = mapName;
        }

        m_model.MapStore.Mappings[stringKey] = maps;
        m_model.MapStore.WriteStore();
        m_model.CurrentMap = stringKey;
    }
}

