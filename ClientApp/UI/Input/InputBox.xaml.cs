using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
using Microsoft.Identity.Client;

namespace Thetacat.UI.Input
{
    /// <summary>
    /// Interaction logic for InputBox.xaml
    /// </summary>
    public partial class InputBox : Window
    {
        private InputBoxModel m_model { get; init; }

        public InputBox(string prompt, string initialText)
        {
            InitializeComponent();

            m_model = new InputBoxModel();
            m_model.Prompt = prompt;
            m_model.InputText = initialText;
            DataContext = m_model;
        }

        private void OkButton(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void CancelButton(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        public static bool FPrompt(string prompt, string initialText, [MaybeNullWhen(false)] out string inputText, Window? parent = null)
        {
            InputBox box = new(prompt, initialText);

            box.Owner = parent;
            if (box.ShowDialog() ?? false)
            {
                inputText = box.m_model.InputText;
                return true;
            }

            inputText = null;
            return false;
        }
    }
}
