using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Thetacat.Model;
using Thetacat.ServiceClient.LocalDatabase;
using Thetacat.Types;
using Thetacat.Util;

namespace Thetacat.Migration.Elements.Versions
{
    /// <summary>
    /// Interaction logic for VersionStacks.xaml
    /// </summary>
    public partial class VersionStacks : UserControl
    {
        private readonly SortableListViewSupport m_sortableListViewSupport;
        private void SortType(object sender, RoutedEventArgs e) => m_sortableListViewSupport.Sort(sender as GridViewColumnHeader);
        private ElementsMigrate? m_migrate;

        private ElementsMigrate _Migrate
        {
            get
            {
                if (m_migrate == null)
                    throw new Exception($"initialize never called on {this.GetType().Name}");
                return m_migrate;
            }
        }

        public VersionStacks()
        {
            InitializeComponent();
            m_sortableListViewSupport = new SortableListViewSupport(diffOpListView);
            diffOpListView.ItemsSource = m_migrateSummaryItems;
        }

        public void Initialize(ElementsDb db, ElementsMigrate migrate)
        {
            m_migrate = migrate;
            _Migrate.StacksMigrate.SetVersionStacks(new List<PseStackItem>(db.ReadVersionStacks()));
            _Migrate.StacksMigrate.SetMediaStacks(new List<PseStackItem>(db.ReadMediaStacks()));

            VersionStackListView.ItemsSource = _Migrate.StacksMigrate.VersionStacks;
            MediaStackListView.ItemsSource = _Migrate.StacksMigrate.MediaStacks;
        }

        public void RebuildStacks()
        {
            _Migrate.StacksMigrate.UpdateStacksWithCatStacks(_Migrate.MediaMigrate);
        }

        private void DoCreateCatStacks(object sender, RoutedEventArgs e)
        {
            m_migrateSummaryItems.Clear();

            // associate every version stack with its cat media item
            List<StackMigrateSummaryItem> summaryItems = _Migrate.StacksMigrate.CreateCatStacks(_Migrate.MediaMigrate);

            foreach (StackMigrateSummaryItem summaryItem in summaryItems)
            {
                m_migrateSummaryItems.Add(summaryItem);
            }
        }

        private readonly ObservableCollection<StackMigrateSummaryItem> m_migrateSummaryItems = new();

        private void DoSummaryKeyDown(object sender, KeyEventArgs e) => CheckableListViewSupport<StackMigrateSummaryItem>.DoKeyDown(diffOpListView, sender, e);

        private void DoMigrate(object sender, RoutedEventArgs e)
        {
            List<StackMigrateSummaryItem> checkedItems = CheckableListViewSupport<StackMigrateSummaryItem>.GetCheckedItems(diffOpListView);

            foreach (StackMigrateSummaryItem checkedItem in checkedItems)
            {
                if (!App.State.Catalog.TryGetMedia(checkedItem.MediaID, out MediaItem? catItem))
                    throw new CatExceptionDataCoherencyFailure($"media not found for summary item: {checkedItem}");

                MediaStacks stacks = App.State.Catalog.GetStacksFromType(checkedItem.StackType);

                // create the stack if necessary
                if (!stacks.Items.TryGetValue(checkedItem.StackID, out MediaStack? stack))
                {
                    stack = new MediaStack(checkedItem.StackType, "");
                    stack.StackId = checkedItem.StackID;
                    stack.PendingOp = MediaStack.Op.Create;
                    stacks.AddStack(stack);
                }

                App.State.Catalog.AddMediaToStackAtIndex(checkedItem.StackType, stack.StackId, checkedItem.MediaID, checkedItem.StackIndex);
            }

            m_migrateSummaryItems.Clear();
            RebuildStacks();
        }
    }
}
