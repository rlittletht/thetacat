using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using HeyRed.Mime;
using Thetacat.Azure;
using Thetacat.Import.UI;
using Thetacat.Logging;
using Thetacat.Metatags.Model;
using Thetacat.Model;
using Thetacat.ServiceClient;
using Thetacat.Types;
using Thetacat.UI;
using Thetacat.Util;

namespace Thetacat.Import;

public class Repather
{
    public static void LaunchRepather(Window parentWindow)
    {
        VirtualRepathing repather = new();
        repather.Owner = parentWindow;
        repather.ShowDialog();
    }
}
