using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Thetacat.Controls;

namespace Thetacat.UI.Controls;

public class MetatagTreeViewTemplateSelector : DataTemplateSelector
{
    public DataTemplate? CheckableTemplate { get; set; }
    public DataTemplate? CheckableTemplateWithValues { get; set; }
    public DataTemplate? NonCheckableTemplate { get; set; }

    public T? ParentOfType<T>(DependencyObject? element) where T : DependencyObject
    {
        if (element == null)
            return default;
        else
            return GetParents(element).OfType<T>().FirstOrDefault();
    }

    public IEnumerable<DependencyObject> GetParents(DependencyObject? element)
    {
        if (element == null)
            throw new ArgumentNullException(nameof(element));
        while ((element = GetParent(element)) != null)
        {
            yield return element;
        }
    }

    private DependencyObject? GetParent(DependencyObject element)
    {
        DependencyObject? parent = VisualTreeHelper.GetParent(element);
        if (parent == null)
        {
            if (element is FrameworkElement frameworkElement)
                parent = frameworkElement.Parent;
        }

        return parent;
    }

    public override DataTemplate? SelectTemplate(object item, DependencyObject container)
    {
        MetatagTreeView? treeView = ParentOfType<MetatagTreeView>(container);

        if (treeView is { Checkable: true })
        {
            if (treeView is { HasValues: true })
                return CheckableTemplateWithValues;
            return CheckableTemplate;

        }

        return NonCheckableTemplate;
    }
}
