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
    public ApplyMetatag? ApplyMetatagPanel { get; set; }

    public void OnCloseCollection()
    {
        ApplyMetatagPanel?.Close();
        ApplyMetatagPanel = null;
    }
}
