﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Thetacat.Model;
using Thetacat.Model.Metatags;

namespace Thetacat.Types;

public interface IAppState
{
    Catalog Catalog { get; set; }
    TcSettings.TcSettings Settings { get; set; }
    MetatagSchema? MetatagSchema { get; set; }
    void RegisterWindowPlace(Window window, string key);
    void RefreshMetatagSchema();
}