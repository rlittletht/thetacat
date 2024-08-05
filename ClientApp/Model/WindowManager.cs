﻿using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using Accessibility;
using Thetacat.Explorer;

namespace Thetacat.Model;

/*----------------------------------------------------------------------------
    %%Class: WindowManager
    %%Qualified: Thetacat.Model.WindowManager

    This class manages all the top level windows. Specifically useful for
    dealing with modeless windows that someone has to clean up, but also
    useful for getting at a top level window
----------------------------------------------------------------------------*/
public class WindowManager
{
    private ApplyMetatag? m_applyMetatag = null;
    private List<MediaItemZoom> m_zooms = new List<MediaItemZoom>();

    public ApplyMetatag? ApplyMetatagPanel
    {
        get => m_applyMetatag;
        set
        {
            if (value != null && value != m_applyMetatag)
                value.Closing += ((_, _) =>
                                  {
                                      App.State.WindowManager.ApplyMetatagPanel = null;
                                  });
            m_applyMetatag = value;
        }
    }

    private void OnZoomClosing(object? sender, CancelEventArgs e)
    {
        if (sender != null)
            m_zooms.Remove((MediaItemZoom)sender);
    }

    /*----------------------------------------------------------------------------
        %%Function: AddZoom
        %%Qualified: Thetacat.Model.WindowManager.AddZoom

        Add a new media zoom top level item
    ----------------------------------------------------------------------------*/
    public void AddZoom(MediaItemZoom zoom)
    {
        m_zooms.Add(zoom);
        zoom.Closing += OnZoomClosing;
    }

    /*----------------------------------------------------------------------------
        %%Function: OnCloseCollection
        %%Qualified: Thetacat.Model.WindowManager.OnCloseCollection

        When closing a media collection, close all the top level windows
    ----------------------------------------------------------------------------*/
    public void OnCloseCollection()
    {
        foreach (MediaItemZoom zoom in m_zooms)
        {
            // must remove our OnZoomClosing to prevent us from modifying the very list
            // we are iterating
            zoom.Closing -= OnZoomClosing;
            zoom.Close();
        }

        ApplyMetatagPanel?.Close();
        ApplyMetatagPanel = null;
    }
}
