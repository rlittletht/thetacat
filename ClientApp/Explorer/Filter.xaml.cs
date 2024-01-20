using Emgu.CV.Dnn;
using MetadataExtractor.Formats.Xmp;
using System;
using System.Collections.Generic;
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
using Thetacat.Explorer;
using Thetacat.Metatags;
using Thetacat.Metatags.Model;
using Thetacat.Standards;

namespace Thetacat.Explorer;

/// <summary>
/// Interaction logic for Filter.xaml
/// </summary>
public partial class Filter : Window
{
    private FilterModel m_model = new();

    public Filter()
    {
        InitializeComponent();
        DataContext = m_model;
        m_model.RootAvailable = new MetatagTree(App.State.MetatagSchema.MetatagsWorking, null, null);
        App.State.RegisterWindowPlace(this, "FilterCatalogWindow");
        Metatags.Initialize(m_model.RootAvailable.Children, 0, MetatagStandards.Standard.User, BuildInitialIndeterminateState());
    }

    Dictionary<string, bool?> BuildInitialIndeterminateState()
    {
        Dictionary<string, bool?> initialState = new();

        if (m_model.RootAvailable == null)
            return initialState;

        foreach (IMetatagTreeItem item in m_model.RootAvailable.Children)
        {
            item.Preorder(
                (visiting, depth) =>
                {
                    initialState.Add(visiting.ID, null);
                },
                0);
        }

        return initialState;
    }

    private void DoApply(object sender, RoutedEventArgs e)
    {
        this.DialogResult = true;
        this.Close();
    }

    public Dictionary<Guid, bool> GetMetatagSetsAndUnsetsForFilter()
    {
        return Metatags.GetCheckedAndUncheckedItems(true/*okToMarkContainer*/);
    }
}