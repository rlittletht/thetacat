﻿using System.Collections.ObjectModel;
using Thetacat.Types;

namespace Thetacat.Metatags;

public interface IMetatagTreeItem
{
    public ObservableCollection<IMetatagTreeItem> Children { get; }
    public string Description { get; }
    public string Name { get; }
    public string ID { get; }
    public IMetatagTreeItem? FindMatchingChild(IMetatagMatcher<IMetatagTreeItem> matcher, int levelsToRecurse);
}
