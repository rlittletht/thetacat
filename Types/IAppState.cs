using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Thetacat.Model;

namespace Thetacat.Types;

public interface IAppState
{
    TcSettings.TcSettings Settings { get; set; }
    MetatagSchema? MetatagSchema { get; set; }
    void RegisterWindowPlace(Window window, string key);
    void RefreshMetatagSchema();
}