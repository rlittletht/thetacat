using System.Windows;
using System.Windows.Controls;

namespace Thetacat.UI.Helpers;

/*----------------------------------------------------------------------------
    %%Function: PanelButtonHelper

    Lots of DependentProperties for a stack panel so that we can set default
    values for the button children

    ApplyButtonPropertes: bool
        the top level "do this or not"

    Spacing: double
        the default inner horizontal spacing for the buttons

    Width: double
        the default width for each button
----------------------------------------------------------------------------*/
public class PanelButtonHelper
{
    public static bool GetApplyButtonProperties(DependencyObject obj) => (bool)obj.GetValue(ApplyButtonPropertiesProperty);
    public static void SetApplyButtonProperties(DependencyObject obj, bool value) => obj.SetValue(ApplyButtonPropertiesProperty, value);

    public static readonly DependencyProperty ApplyButtonPropertiesProperty =
        DependencyProperty.RegisterAttached("ApplyButtonProperties", typeof(bool), typeof(PanelButtonHelper), new PropertyMetadata(false, OnApplyButtonPropertiesChanged));

    public static double GetSpacing(DependencyObject obj) => (double)obj.GetValue(SpacingProperty);
    public static void SetSpacing(DependencyObject obj, double value) => obj.SetValue(SpacingProperty, value);

    public static readonly DependencyProperty SpacingProperty =
        DependencyProperty.RegisterAttached("Spacing", typeof(double), typeof(PanelButtonHelper), new PropertyMetadata(-1.0));

    public static double GetWidth(DependencyObject obj) => (double)obj.GetValue(WidthProperty);
    public static void SetWidth(DependencyObject obj, double value) => obj.SetValue(WidthProperty, value);

    public static readonly DependencyProperty WidthProperty =
        DependencyProperty.RegisterAttached("Width", typeof(double), typeof(PanelButtonHelper), new PropertyMetadata(-1.0));


    private static void OnApplyButtonPropertiesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is Panel panel)
        {
            double margin = GetSpacing(d);
            double width = GetWidth(d);

            panel.Loaded += (s, s2) =>
                            {
                                foreach (UIElement child in panel.Children)
                                {
                                    if (child is Button button)
                                    {
                                        if (margin >= 0.0)
                                            UpdateMargin(button, panel.Children.IndexOf(button), panel.Children.Count, margin);
                                        if (width >= 0.0)
                                            button.Width = width;
                                    }
                                }
                            };
        }
    }

//    private static void OnSpacingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
//    {
//        if (d is Panel panel)
//        {
//            double margin = GetSpacing(d);
//
//            // don't apply this if we don't have a margin set...
//            if (margin < 0.0)
//                return;
//
//            panel.Loaded += (s, s2) =>
//                            {
//                                foreach (UIElement child in panel.Children)
//                                {
//                                    if (child is Button button)
//                                    {
//                                        UpdateMargin(button, panel.Children.IndexOf(button), panel.Children.Count, margin);
//                                    }
//                                }
//                            };
//        }
//    }

    private static void UpdateMargin(Button button, int index, int count, double margin)
    {
        if (index == 0) // First button
            button.Margin = new Thickness(0, 0, margin, 0);
        else if (index == count - 1) // Last button
            button.Margin = new Thickness(margin, 0, 0, 0);
        else // Middle buttons
            button.Margin = new Thickness(margin, 0, margin, 0);
    }
}
