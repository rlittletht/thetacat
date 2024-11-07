using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
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

namespace Thetacat.UI.Input;

/// <summary>
/// Interaction logic for InputFormats.xaml
/// </summary>
public partial class InputFormats : Window
{
    private InputFormatsModel m_model { get; init; }

    public string m_result = string.Empty;

    public string Result => m_result;

    public InputFormats(string prompt, string initialText)
    {
        InitializeComponent();

        m_model = new InputFormatsModel();
        m_model.Prompt = prompt;

        // figure out what kind of content it is
        if (DateTime.TryParse(initialText, out DateTime dateTime))
        {
            m_model.InputDate = dateTime.ToString("G");
        }
        else if (double.TryParse(initialText, out double d))
        {
            m_model.InputNumber = d.ToString(CultureInfo.InvariantCulture);
        }
        else
        {
            m_model.InputText = initialText;
        }

        DataContext = m_model;
    }

    private void OkButton(object sender, RoutedEventArgs e)
    {
        // in specifity order...
        m_result = "";

        if (!string.IsNullOrEmpty(m_model.InputDate))
        {
            if (!DateTime.TryParse(m_model.InputDate, out DateTime dateTime))
            {
                MessageBox.Show($"Could not parse {m_model.InputDate} as a date/time");
                return;
            }

            m_result = dateTime.ToUniversalTime().ToString("u");
        }

        if (!string.IsNullOrEmpty(m_model.InputNumber))
        {
            if (m_result != "")
            {
                MessageBox.Show($"Can't specify multiple values");
                return;
            }

            if (!double.TryParse(m_model.InputNumber, out double d))
            {
                MessageBox.Show($"Could not parse {m_model.InputNumber} as a number");
                return;
            }

            m_result = d.ToString(CultureInfo.InvariantCulture);
        }

        if (!string.IsNullOrEmpty(m_model.InputText))
        {
            if (m_result != "")
            {
                MessageBox.Show($"Can't specify multiple values");
                return;
            }

            m_result = m_model.InputText;
        }

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
        InputFormats box = new(prompt, initialText);

        box.Owner = parent;
        if (box.ShowDialog() ?? false)
        {
            inputText = box.Result;
            return true;
        }

        inputText = null;
        return false;
    }
}