using Thetacat.Migration.Elements.Media;

namespace Thetacat.Migration.Elements;

public class ElementsMigrate
{
    public delegate void SwitchToTabDelegate();
    public delegate void ReloadSchemasDelegate();

    public SwitchToTabDelegate SwitchToSchemaSummariesTab { get; }
    public SwitchToTabDelegate SwitchToMediaTagsSummariesTab { get; }
    public ReloadSchemasDelegate ReloadSchemas { get; }

    public MediaMigrate MediaMigrate { get; }
    public MetatagMigrate MetatagMigrate { get; }
    public StacksMigrate StacksMigrate { get; }

    public ElementsMigrate(
        MetatagMigrate metatagMigrate, 
        MediaMigrate mediaMigrate, 
        StacksMigrate stacksMigrate,
        SwitchToTabDelegate switchToSchemaSummariesTabDelegate, 
        SwitchToTabDelegate switchToMediaTagsSummariesTabDelegate,
        ReloadSchemasDelegate reloadSchemasDelegate)
    {
        SwitchToSchemaSummariesTab = switchToSchemaSummariesTabDelegate;
        SwitchToMediaTagsSummariesTab = switchToMediaTagsSummariesTabDelegate;
        ReloadSchemas = reloadSchemasDelegate;

        MediaMigrate = mediaMigrate;
        MetatagMigrate = metatagMigrate;
        StacksMigrate = stacksMigrate;
    }
}
