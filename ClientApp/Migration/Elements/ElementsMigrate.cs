using Thetacat.Migration.Elements.Media;

namespace Thetacat.Migration.Elements;

public class ElementsMigrate
{
    public MediaMigrate MediaMigrate { get; }
    public MetatagMigrate MetatagMigrate { get; }
    public StacksMigrate StacksMigrate { get; }

    public ElementsMigrate(MetatagMigrate metatagMigrate, MediaMigrate mediaMigrate, StacksMigrate stacksMigrate)
    {
        MediaMigrate = mediaMigrate;
        MetatagMigrate = metatagMigrate;
        StacksMigrate = stacksMigrate;
    }
}
