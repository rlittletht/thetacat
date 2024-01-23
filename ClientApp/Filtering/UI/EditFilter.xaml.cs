using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
using Thetacat.Metatags;
using Thetacat.Metatags.Model;
using Thetacat.Standards;
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

        private Dictionary<Guid, string>? m_metatagLineageMap;

        public static Dictionary<Guid, string> BuildLineageMap(MetatagStandards.Standard standard)
        {
            // for now, just the user tags
            IMetatagTreeItem? standardRoot = App.State.MetatagSchema.GetStandardRootItem(MetatagStandards.Standard.User);
            Dictionary<Guid, string> lineage = new();

            if (standardRoot != null)
            {
                standardRoot.Preorder(
                    null,
                    (treeItem, parent, depth) =>
                    {
                        string dropdownName;

                        if (parent != null)
                        {
                            Guid parentID = Guid.Parse(parent.ID);

                            lineage.TryAdd(parentID, parent.Name);
                            dropdownName = $"{lineage[parentID]}:{treeItem.Name}";
                        }
                        else
                        {
                            dropdownName = treeItem.Name;
                        }

                        lineage.Add(Guid.Parse(treeItem.ID), dropdownName);
                    },
                    0);
            }

            return lineage;
        }

        private void InitializeAvailableTags()
        {
            if (m_metatagLineageMap == null)
                m_metatagLineageMap = BuildLineageMap(MetatagStandards.Standard.User);

            IComparer<KeyValuePair<Guid, string>> comparer = Comparer<KeyValuePair<Guid, string>>.Create((x, y) => String.Compare(x.Value, y.Value, StringComparison.Ordinal));
            ImmutableSortedSet<KeyValuePair<Guid, string>> sorted = m_metatagLineageMap.ToImmutableSortedSet(comparer);

            foreach (KeyValuePair<Guid, string> item in sorted)
            {
                m_model.AvailableTags.Add(new FilterModelMetatagItem(App.State.MetatagSchema.GetMetatagFromId(item.Key)!, item.Value));
            }
        }

        public EditFilter(PostfixText? expression = null, Dictionary<Guid, string>? lineageMap = null)
        {
            DataContext = m_model;
            InitializeComponent();

            m_metatagLineageMap = lineageMap;
            m_model.Expression = expression ?? new PostfixText();

            InitializeAvailableTags();
            UpdateQueryClauses();
        }

        void UpdateQueryClauses()
        {
            m_model.ExpressionClauses.Clear();
            m_model.ExpressionClauses.AddRange(m_model.Expression.ToStrings(
                (field) =>
                {
                    if (m_metatagLineageMap != null && Guid.TryParse(field, out Guid metatagId))
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

            m_model.Expression.AddExpression(
                Expression.Create(
                    Value.CreateForField(m_model.SelectedTagForClause.Metatag.ID.ToString("B")),
                    Value.Create(m_model.ValueForClause),
                    m_model.ComparisonOpForClause));

            UpdateQueryClauses();
        }

        private void DoCancel(object sender, RoutedEventArgs e)
        {
            Close();
        }

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
    }
}
