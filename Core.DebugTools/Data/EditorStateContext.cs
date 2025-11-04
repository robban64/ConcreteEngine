using Core.DebugTools.Definitions;

namespace Core.DebugTools.Data;

internal sealed class EditorStateContext
{
    public EditorViewMode ViewMode { get; set; } = EditorViewMode.None;
    public SidebarEditorMode  SidebarMode { get; set; } = SidebarEditorMode.None;
    
    public AssetStoreViewModel AssetViewModel { get; } = new();

    private MetricService _metricService;

    public EditorStateContext(MetricService metricService)
    {
        _metricService = metricService;
    }

    public void ExecuteCommand(string name, string? args1, string? args2)
    {
    }
    
    public void SetViewMode(EditorViewMode mode)
    {
        if(mode == ViewMode) return;
        ViewMode = mode;

        _metricService.ToogleMetrics(ViewMode == EditorViewMode.Metrics);

    }
    
    public void SetSidebarMode(SidebarEditorMode mode)
    {
        //if(mode == SidebarMode) return;
        SidebarMode = mode;
    }


}