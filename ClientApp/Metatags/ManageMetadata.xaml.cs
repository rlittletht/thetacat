using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using Thetacat.Metatags.Model;
using Thetacat.ServiceClient;
using Thetacat.Types;

namespace Thetacat.Metatags
{
    /// <summary>
    /// Interaction logic for ManageMetadata.xaml
    /// </summary>
    public partial class ManageMetadata : Window
    {
        public ManageMetadata()
        {
            InitializeComponent();
            App.State.RegisterWindowPlace(this, "ManageMetadata");
        }

        MetatagSchema LoadSampleSchema()
        {
            Guid root1 = Guid.NewGuid();
            Guid root2 = Guid.NewGuid();
            Guid root1_child1 = Guid.NewGuid();
            Guid root1_child2 = Guid.NewGuid();
            Guid root2_child1 = Guid.NewGuid();
            Guid root2_child1_child1 = Guid.NewGuid();
            Guid root2_child1_child2 = Guid.NewGuid();

            ServiceMetatagSchema serviceMetatagSchema =
                new()
                {
                    Metatags =
                        new()
                        {
                            new ServiceMetatag()
                            {
                                Description = "Root1 Metatag", ID = root1, Name = "Root1",
                                Parent = null
                            },
                            new ServiceMetatag()
                            {
                                Description = "Root2 Metatag", ID = root2, Name = "Root2",
                                Parent = null
                            },
                            new ServiceMetatag()
                            {
                                Description = "R1C1 Metatag", ID = root1_child1, Name = "R1-C1",
                                Parent = root1
                            },
                            new ServiceMetatag()
                            {
                                Description = "R1C2 Metatag", ID = root1_child2, Name = "R1-C2",
                                Parent = root1
                            },
                            new ServiceMetatag()
                            {
                                Description = "R2C1 Metatag", ID = root2_child1, Name = "R2-C1",
                                Parent = root2
                            },
                            new ServiceMetatag()
                            {
                                Description = "R2C1C1 Metatag", ID = root2_child1_child1,
                                Name = "R1-C1-C1", Parent = root2_child1
                            },
                            new ServiceMetatag()
                            {
                                Description = "R2C1C2 Metatag", ID = root2_child1_child2,
                                Name = "R1-C1-C2", Parent = root2_child1
                            }
                        },
                    SchemaVersion = 1
                };

            MetatagSchema schema = new MetatagSchema();
            schema.ReplaceFromService(serviceMetatagSchema);
            return schema;
        }

        private void LoadMetatags(object sender, RoutedEventArgs e)
        {
            App.State.RefreshMetatagSchema();
            MetatagsTree.Initialize(App.State.MetatagSchema.WorkingTree.Children, App.State.MetatagSchema.SchemaVersionWorking);
        }
    }
}
