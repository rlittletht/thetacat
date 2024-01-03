﻿using System.Collections.ObjectModel;
using Thetacat.Model.Metatags;

namespace Thetacat.UI.Explorer;

public class ApplyMetatagModel
{
    private ObservableCollection<Metatag> m_metatags = new();

    public ObservableCollection<Metatag> Metatags { get; }
    public ObservableCollection<Metatag> MetatagsApplied { get; }

    public void Set(MetatagSchema schema)
    {

    }
}
