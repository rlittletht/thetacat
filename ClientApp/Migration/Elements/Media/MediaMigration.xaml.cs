using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Packaging;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Thetacat.Controls;
using Thetacat.Migration.Elements.Media;
using Thetacat.Migration.Elements.Metadata;
using Thetacat.Types;

namespace Thetacat.Migration.Elements;

/// <summary>
/// Interaction logic for MediaMigration.xaml
/// </summary>
public partial class MediaMigration : UserControl
{
    private IAppState? m_appState;
    private List<MediaItem>? m_items;
    private readonly List<PathSubstitution> m_pathSubstitutions = new();
    private MetatagMigrate? m_migrate;

    public MediaMigration()
    {
        InitializeComponent();
    }

    private void VerifyPaths(object sender, RoutedEventArgs e)
    {
        VerifyPaths();
    }

    private void DoFilterItemChanged(object sender, SelectionChangedEventArgs e)
    {
        CollectionViewSource.GetDefaultView(mediaItemsListView.ItemsSource).Refresh();
    }

    public void Initialize(IAppState appState, ElementsDb db, MetatagMigrate migrate)
    {
        m_appState = appState;
        m_items = db.ReadMediaItems();
        m_migrate = migrate;
        
        mediaItemsListView.ItemsSource = m_items;

        CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(mediaItemsListView.ItemsSource);
        view.Filter = FilterMediaItem;

        mediaItemsListView.ItemsSource = m_items;
        SetSubstitutionsFromSettings();
    }

    bool FilterMediaItem(object o)
    {
        MediaItem item = (MediaItem)o;

        if (FilterItems.SelectedItem == null)
            return true;

        switch (((ComboBoxItem)FilterItems.SelectedItem).Content as string)
        {
            case "All":
                return true;
            case "Maybe":
                return (item.PathVerified == TriState.Maybe);
            case "Yes":
                return (item.PathVerified == TriState.Yes);
            case "No":
                return (item.PathVerified == TriState.No);
        }

        return true;
    }

    public void SetSubstitutionsFromSettings()
    {
        if (m_appState == null)
            throw new Exception("Not initialized");

        foreach (string s in m_appState.Settings.Settings.RgsValue("LastElementsSubstitutions"))
        {
            string[] pair = s.Split(",");
            if (pair.Length != 2)
            {
                MessageBox.Show($"bad subst setting in registry: {s}");
                continue;
            }

            m_pathSubstitutions.Add(new PathSubstitution { From = pair[0], To = pair[1] });
        }

        substDatagrid.ItemsSource = m_pathSubstitutions;
    }

    private int m_countRunningVerifyTasks = 0;

    void SetVerifyResult()
    {
        TriState tri = TriState.Maybe;

        if (m_items != null)
        {
            foreach (MediaItem item in m_items)
            {
                if (item.PathVerified == TriState.No)
                    tri = TriState.No;

                if (item.PathVerified == TriState.Yes && tri != TriState.No)
                    tri = TriState.Yes;

                if (item.PathVerified == TriState.Maybe && tri != TriState.No)
                    tri = TriState.Maybe;
            }
        }

        switch (tri)
        {
            case TriState.Maybe:
                VerifyResult.Text = "?";
                VerifyResult.Foreground = new SolidColorBrush(Colors.Black);
                break;
            case TriState.Yes:
                VerifyResult.Text = "+";
                VerifyResult.Foreground = new SolidColorBrush(Colors.Green);
                break;
            case TriState.No:
                VerifyResult.Text = "X";
                VerifyResult.Foreground = new SolidColorBrush(Colors.Red);
                break;
        }

        VerifyResult.Visibility = Visibility.Visible;
    }

    void CompleteVerifyTask()
    {
        if (VerifyStatus == null)
            return;

        if (Interlocked.Decrement(ref m_countRunningVerifyTasks) == 0)
        {
            VerifyStatus.Visibility = Visibility.Collapsed;
            ((Storyboard?)VerifyStatus.Resources.FindName("spinner"))?.Stop();
            SetVerifyResult();
        }
    }

    void VerifyPathSet(int start, int end, Dictionary<string, string> subs)
    {
        Debug.Assert(m_items != null, nameof(m_items) + " != null");
        Debug.Assert(m_appState != null, nameof(m_appState) + " != null");

        Interlocked.Increment(ref m_countRunningVerifyTasks);
        TaskScheduler uiScheduler = TaskScheduler.FromCurrentSynchronizationContext();

        Task.Run(
                () =>
                {
                    for (int i = start; i < end; i++)
                    {
                        m_items[i].CheckPath(m_appState, subs);
                    }
                })
           .ContinueWith(delegate { CompleteVerifyTask(); }, uiScheduler);
    }

    public void VerifyPaths()
    {
        if (m_appState == null)
            throw new Exception("Not initialized");

        if (m_items == null)
            return;

        Dictionary<string, string> pathSubst = new();

        List<string> regValues = new();

        foreach (PathSubstitution sub in m_pathSubstitutions)
        {
            pathSubst.Add(sub.From, sub.To);
            regValues.Add($"{sub.From},{sub.To}");
        }

        // persist the paths to the registry here
        m_appState.Settings.Settings.SetRgsValue("LastElementsSubstitutions", regValues.ToArray());
        m_appState.Settings.Settings.Save();

        ((Storyboard?)VerifyStatus.Resources.FindName("spinner"))?.Begin();

        VerifyResult.Visibility = Visibility.Hidden;
        VerifyStatus.Visibility = Visibility.Visible;

        // split the list into 4 parts and do them in parallel
        int segLength = m_items.Count; //  / 10;
        int segStart = 0;
        for (int iSeg = 0; iSeg < 10; iSeg++)
        {
            int segEnd = Math.Min(segStart + segLength, m_items.Count);

            VerifyPathSet(segStart, segEnd, pathSubst);
            segStart += segLength;

            if (segEnd == m_items.Count)
                break;
        }

        if (segStart < m_items.Count)
            VerifyPathSet(segStart, m_items.Count, pathSubst);
    }
}