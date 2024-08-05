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
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Thetacat.Model;
using Thetacat.Types;
using Thetacat.Util;

namespace Thetacat.Explorer.UI
{
    /// <summary>
    /// Interaction logic for SelectStack.xaml
    /// </summary>
    public partial class SelectStack : Window
    {
        private readonly SelectStackModel _model = new SelectStackModel();

        private MediaItem? m_itemStackingWith;

        private MediaItem _ItemStackingWith => m_itemStackingWith ?? throw new CatExceptionInternalFailure("no stacking item specified");


        public SelectStack()
        {
            DataContext = _model;
            InitializeComponent();
            _model.PropertyChanged += ModelOnPropertyChanged;
        }

        private void ModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "CurrentStack")
            {
                _model.CurrentType = _model.CurrentStack?.Type ?? MediaStackType.Media;
                _model.Description = _model.CurrentStack?.Description ?? "";
                _model.StackId = _model.CurrentStack?.StackId.ToString() ?? string.Empty;
            }
        }

        private void CreateStack(object sender, RoutedEventArgs e)
        {
            _model.CurrentStack = null;
            _model.StackId = Guid.NewGuid().ToString();
            _model.Description = "";
            _model.CurrentType = MediaStackType.Media;
        }

        private void DoSave(object sender, RoutedEventArgs e)
        {
            if (_model.CurrentStack == null)
            {
                bool fMediaStack = _model.CurrentType.Equals(MediaStackType.Media);

                if ((fMediaStack && _ItemStackingWith.MediaStack != null)
                    || (!fMediaStack && _ItemStackingWith.VersionStack != null))
                {
                    MediaStack existing =
                        fMediaStack
                            ? App.State.Catalog.MediaStacks.Items[_ItemStackingWith.MediaStack!.Value]
                            : App.State.Catalog.VersionStacks.Items[_ItemStackingWith.VersionStack!.Value];

                    MediaStackItem? existingStackItem = existing.FindMediaInStack(_ItemStackingWith.ID);

                    if (existingStackItem == null)
                    {
                        MessageBox.Show("Couldn't find item in stack it belonged to!");
                        this.DialogResult = false;
                        this.Close();
                    }

                    if (MessageBox.Show(
                            $"This item already has a {_model.CurrentType} stack: '{existing.Description}'. Do you want to replace that stack with this new stack?",
                            "Confirm replace",
                            MessageBoxButton.YesNo)
                        == MessageBoxResult.No)
                    {
                        this.DialogResult = false;
                        this.Close();
                    }

                    // remove from the existing stack

                    existing.RemoveItem(existingStackItem!);
                }

                MediaStack newStack =
                    fMediaStack
                        ? App.State.Catalog.MediaStacks.CreateNewStack(_model.Description)
                        : App.State.Catalog.VersionStacks.CreateNewStack(_model.Description);

                newStack.PushNewItem(m_itemStackingWith.ID);
                if (fMediaStack)
                    m_itemStackingWith.SetMediaStackVerify(App.State.Catalog, newStack.StackId);
                else
                    m_itemStackingWith.SetVersionStackVerify(App.State.Catalog, newStack.StackId);

                _model.AvailableStacks.Add(newStack);
                _model.CurrentStack = newStack;
            }

            DialogResult = true;
            Close();
        }

        public static MediaStack? GetMediaStack(Window? owner, MediaItem item)
        {
            SelectStack stack = new SelectStack();

            if (owner != null)
                stack.Owner = owner;

            stack.m_itemStackingWith = item;

            if (item.VersionStack != null)
                stack._model.AvailableStacks.Add(App.State.Catalog.VersionStacks.Items[item.VersionStack.Value]);
            if (item.MediaStack != null)
                stack._model.AvailableStacks.Add(App.State.Catalog.MediaStacks.Items[item.MediaStack.Value]);

            if (stack.ShowDialog() is true)
                return stack._model.CurrentStack;

            return null;
        }
    }
}
