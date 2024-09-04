using System.Windows;
using Thetacat.Explorer;
using Thetacat.Filtering;
using Thetacat.Model;
using Thetacat.Types;

namespace Thetacat.MainApp;

public interface IMainCommands
{
    Window Window { get; }
    Filter? CurrentFilter { get; }
    MediaExplorer MediaExplorer { get; }
    MediaExplorerCollection MediaExplorerCollection { get; }

    void RebuildProfileList();
    public void ToggleAsyncLog();
    public void ToggleAppLog();
    public void SetPreviewSize(ExplorerItemSize size);
    public void RebuildTimeline();
    public void SetTimelineType(TimelineType type);
    public void SetTimelineOrder(TimelineOrder order);
    public void ChooseFilterOrCurrent(Filter? filter);
    public void EmptyTrash();

}
