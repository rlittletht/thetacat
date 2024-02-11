using System;
using Thetacat.TcSettings;

namespace Thetacat.Types;

public class ProfileChangedEventArgs : EventArgs
{
    public Profile Profile { get; init; }

    public ProfileChangedEventArgs(Profile profile)
    {
        Profile = profile;
    }
}
