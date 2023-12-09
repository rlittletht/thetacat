using Thetacat.Migration.Elements.Media;

namespace Thetacat.Migration.Elements;

public class ElementsMigrate
{
    public MediaMigrate MediaMigrate { get; }
    public MetatagMigrate MetatagMigrate { get; }

    public ElementsMigrate(MetatagMigrate metatagMigrate, MediaMigrate mediaMigrate)
    {
        MediaMigrate = mediaMigrate;
        MetatagMigrate = metatagMigrate;
    }
}
