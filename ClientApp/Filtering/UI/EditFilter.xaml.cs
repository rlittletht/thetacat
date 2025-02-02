﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using TCore.PostfixText;
using Thetacat.Logging;
using Thetacat.Metatags;
using Thetacat.Metatags.Model;
using Thetacat.Model;
using Thetacat.Model.Mediatags;
using Thetacat.Standards;
using Thetacat.Types;
using Thetacat.Util;
using Expression = TCore.PostfixText.Expression;

namespace Thetacat.Filtering.UI
{
    /// <summary>
    /// Interaction logic for EditFilter.xaml
    /// </summary>
    public partial class EditFilter : Window
    {
        private readonly EditFilterModel m_model = new EditFilterModel();

        private readonly Dictionary<Guid, string> m_metatagLineageMap;
        private readonly MetatagSchema m_filterSchema;

        /*----------------------------------------------------------------------------
            %%Function: InitializeAvailableTags
            %%Qualified: Thetacat.Filtering.UI.EditFilter.InitializeAvailableTags

            Initialize the available tags in the dialog, both the dropdown and the
            popup tree
        ----------------------------------------------------------------------------*/
        private void InitializeAvailableTags()
        {
            App.LogForApp(EventType.Verbose, "ensure builtin done");

            IComparer<KeyValuePair<Guid, string>> comparer =
                Comparer<KeyValuePair<Guid, string>>.Create((x, y) => String.Compare(x.Value, y.Value, StringComparison.Ordinal));
            ImmutableSortedSet<KeyValuePair<Guid, string>> sorted = m_metatagLineageMap.ToImmutableSortedSet(comparer);

            foreach (KeyValuePair<Guid, string> item in sorted)
            {
                m_model.AvailableTags.Add(new FilterModelMetatagItem(m_filterSchema.GetMetatagFromId(item.Key)!, item.Value));
            }

            TagMetatagsTree.Initialize(
                m_filterSchema.WorkingTree.Children,
                m_filterSchema.SchemaVersionWorking);
        }

        /*----------------------------------------------------------------------------
            %%Function: EditFilter
            %%Qualified: Thetacat.Filtering.UI.EditFilter.EditFilter
        ----------------------------------------------------------------------------*/
        public EditFilter(MetatagSchema filterSchema, Dictionary<Guid, string> lineageMap, Filter? filter = null)
        {
            DataContext = m_model;
            InitializeComponent();

            m_filterSchema = filterSchema;
            m_metatagLineageMap = lineageMap;
            if (filter!= null)
            {
                m_model.Expression = filter.Definition.Expression;
                m_model.FilterName = filter.Definition.FilterName;
                m_model.Description = filter.Definition.Description;
                m_model.Id = filter.Id;
                m_model.IsTypeAvailable = false;
                m_model.SelectedType = filter.FilterType == FilterType.Workgroup ? "Workgroup" : "Local";
            }
            else
            {
                m_model.Expression = new PostfixText();
                m_model.IsTypeAvailable = true;
                m_model.Id = Guid.NewGuid();
            }

            InitializeAvailableTags();
            UpdateQueryClauses();
            m_model.PropertyChanged += OnModelPropertyChanged;
            m_model.Types.Add("Local");
            m_model.Types.Add("Workgroup");
        }

        private void OnModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "SelectedTagForClause")
                UpdateClauseOperatorAndValues();
        }

        void UpdateMapCount<T>(Dictionary<T, int> mapCount, T key) where T : notnull
        {
            if (mapCount.TryAdd(key, 1))
                return;
            mapCount[key]++;
        }

        private void UpdateClauseOperatorAndValues()
        {
            m_model.ValuesForClause.Clear();
            m_model.ComparisonOperators.Clear();

            if (m_model.SelectedTagForClause == null)
                return;

            // figure out all the possible values for the tag they just selected

            // get the metatag selected
            Metatag metatag = m_model.SelectedTagForClause.Metatag;
            Dictionary<string, int> valueCounts = new();

            HashSet<ComparisonOperator.Op> ops = new HashSet<ComparisonOperator.Op>();

            foreach (MediaItem item in App.State.Catalog.GetMediaCollection())
            {
                if (item.TryGetMediaTag(metatag.ID, out MediaTag? mediaTag))
                {
                    if (mediaTag.Value == null)
                    {
                        UpdateMapCount(valueCounts, "$false");
                        UpdateMapCount(valueCounts, "$true");

                        ops.Add(ComparisonOperator.Op.Eq);
                        ops.Add(ComparisonOperator.Op.Ne);
                    }
                    else
                    {
                        // don't add more than 20 different values
                        UpdateMapCount(valueCounts, mediaTag.Value);
                        ops.Add(ComparisonOperator.Op.Eq);
                        ops.Add(ComparisonOperator.Op.Rex);
                        ops.Add(ComparisonOperator.Op.Ne);
                        ops.Add(ComparisonOperator.Op.Gt);
                        ops.Add(ComparisonOperator.Op.Gte);
                        ops.Add(ComparisonOperator.Op.Lt);
                        ops.Add(ComparisonOperator.Op.Lte);
                        ops.Add(ComparisonOperator.Op.SEq);
                        ops.Add(ComparisonOperator.Op.SRex);
                        ops.Add(ComparisonOperator.Op.SNe);
                        ops.Add(ComparisonOperator.Op.SGt);
                        ops.Add(ComparisonOperator.Op.SGte);
                        ops.Add(ComparisonOperator.Op.SLt);
                        ops.Add(ComparisonOperator.Op.SLte);
                    }
                }
            }

            if (ops.Count == 0)
            {
                UpdateMapCount(valueCounts, "$false");
                UpdateMapCount(valueCounts, "$true");

                ops.Add(ComparisonOperator.Op.Eq);
                ops.Add(ComparisonOperator.Op.Ne);
            }

            IComparer<KeyValuePair<string, int>> comparer =
                Comparer<KeyValuePair<string, int>>.Create(
                    (x, y) =>
                    {
                        return x.Value - y.Value < 0
                            ? x.Value - y.Value
                            : x.Value - y.Value + 1;
                    });
            ImmutableSortedSet<KeyValuePair<string, int>> sorted = valueCounts.ToImmutableSortedSet(comparer);

            // add the top 15 items and the bottom 5 items
            int groupCount = Math.Min(15, sorted.Count);
            if (groupCount > 0)
            {
                for (int i = 0; i < groupCount; i++)
                {
                    m_model.ValuesForClause.Add(sorted[i].Key);
                }
            }

            groupCount = Math.Min(5, groupCount - 15);

            if (groupCount > 0)
            {
                for (int i = sorted.Count - groupCount; i < sorted.Count; i++)
                {
                    m_model.ValuesForClause.Add(sorted[i].Key);
                }
            }

            foreach (ComparisonOperator.Op op in ops)
            {
                m_model.ComparisonOperators.Add(new ComparisonOperator(op));
            }
        }

        void UpdateQueryClauses()
        {
            m_model.ExpressionClauses.Clear();
            m_model.ExpressionClauses.AddRange(
                m_model.Expression.ToStrings(
                    (field) =>
                    {
                        if (Guid.TryParse(field, out Guid metatagId))
                        {
                            if (m_metatagLineageMap.TryGetValue(metatagId, out string? lineage))
                                return $"[{lineage}]";
                        }

                        return $"[{field}]";
                    }));
        }

        private void AddPostfixOp(object sender, RoutedEventArgs e)
        {
            try
            {
                m_model.Expression.AddOperator(m_model.PostfixOpForClause);
                UpdateQueryClauses();
            }
            catch (Exception exc)
            {
                MessageBox.Show($"Couldn't add operator: {exc}");
            }
        }

        private void AddClause(object sender, RoutedEventArgs e)
        {
            if (m_model.SelectedTagForClause == null)
            {
                MessageBox.Show("Must select a metatag to compare against");
                return;
            }

            if (m_model.ComparisonOpForClause == null)
            {
                MessageBox.Show("Must select a comparison operator");
                return;
            }

            string valueText = m_model.ValueTextForClause;
            Value value;

            // see if this might be a date
            if (DateTime.TryParse(valueText, out DateTime date))
                value = Value.Create(date);
            else if (Int32.TryParse(valueText, out int intValue))
                value = Value.Create(intValue);
            else
                value = Value.Create(valueText);

            m_model.Expression.AddExpression(
                Expression.Create(
                    Value.CreateForField(m_model.SelectedTagForClause.Metatag.ID.ToString("B")),
                    value,
                    m_model.ComparisonOpForClause));

            UpdateQueryClauses();
        }

        private void DoCancel(object sender, RoutedEventArgs e)
        {
            Close();
        }

        public FilterType GetFilterType()
        {
            if (m_model.SelectedType == "Local")
                return FilterType.Local;
            else if (m_model.SelectedType == "Workgroup")
                return FilterType.Workgroup;
            else
                throw new CatExceptionInternalFailure("Unknown filter type");
        }

        public Guid GetId() => m_model.Id;

        public FilterDefinition GetDefinition()
        {
            return new FilterDefinition(m_model.FilterName, m_model.Description, m_model.Expression.ToString());
        }

        private void DoSave(object sender, RoutedEventArgs e)
        {
            // let's make sure the expression is valid
            int values = m_model.Expression.ValuesRemainingAfterReduce();

            if (values != 1)
            {
                MessageBox.Show($"Expression isn't valid -- it reduces to {values}. It must reduce to a single value");
                return;
            }

            if (string.IsNullOrWhiteSpace(m_model.FilterName))
            {
                MessageBox.Show("Must specify a Filter Name before saving");
                return;
            }

            if (string.IsNullOrEmpty(m_model.Description))
            {
                MessageBox.Show("Must specify a description for this filter");
                return;
            }

            DialogResult = true;
            Close();
        }

        private void PopClause(object sender, RoutedEventArgs e)
        {
            m_model.Expression.Pop();
            UpdateQueryClauses();
        }

        private void ChooseTag(object sender, RoutedEventArgs e)
        {
            TagPickerPopup.IsOpen = !TagPickerPopup.IsOpen;
        }

        private void SelectMetatag(Guid parentId)
        {
            foreach (FilterModelMetatagItem tag in m_model.AvailableTags)
            {
                if (tag.Metatag.ID == parentId)
                {
                    m_model.SelectedTagForClause = tag;
                    break;
                }
            }
        }

        private void DoSelectedTagChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is MetatagTreeItem newItem)
            {
                SelectMetatag(newItem.ItemId);
            }

            TagPickerPopup.IsOpen = false;
        }

        private void EditExpression(object sender, RoutedEventArgs e)
        {
            m_model.ExpressionEditing = m_model.Expression.ToString();
            m_model.IsEditingExpression = true;
        }

        private void CancelEditExpression(object sender, RoutedEventArgs e)
        {
            m_model.IsEditingExpression = false;
        }

        private void SaveEditExpression(object sender, RoutedEventArgs e)
        {
            m_model.Expression = PostfixText.CreateFromParserClient(new StringParserClient(m_model.ExpressionEditing));
            UpdateQueryClauses();
            m_model.IsEditingExpression = false;
        }
    }
}
