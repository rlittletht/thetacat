﻿using System;
using System.Windows;

namespace Thetacat.Util;

public static class ThreadContext
{
    public static void InvokeOnUiThread(Action action)
    {
        if (Application.Current.Dispatcher.CheckAccess())
        {
            action();
        }
        else
        {
            Application.Current.Dispatcher.Invoke(action);
        }
    }

    public static void BeginInvokeOnUiThread(Action action)
    {
        if (Application.Current.Dispatcher.CheckAccess())
        {
            action();
        }
        else
        {
            Application.Current.Dispatcher.BeginInvoke(action);
        }
    }
}