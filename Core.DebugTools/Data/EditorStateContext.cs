using Core.DebugTools.Definitions;

namespace Core.DebugTools.Data;

internal sealed class EditorStateContext
{

    public EditorViewMode ViewMode { get; private set; } = EditorViewMode.None;
    public SidebarEditorMode  SidebarMode { get; private set; } = SidebarEditorMode.None;
    
    public AssetStoreViewModel AssetViewModel { get; } = new();
    public EntityListViewModel EntityListViewModel { get; } = new();

    private readonly MetricService _metricService;

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
        if(mode == SidebarMode) return;
        SidebarMode = mode;

        if(mode != SidebarEditorMode.Assets)  AssetViewModel.ResetState();
        if (mode != SidebarEditorMode.Entities) EntityListViewModel.ResetState();
        
        switch (mode)
        {
            case SidebarEditorMode.Assets:
                EditorTable.FillAssetStoreView?.Invoke(AssetViewModel.TypeSelection, AssetViewModel.AssetObjects);
                break;
            case SidebarEditorMode.Entities:
                EditorTable.FillEntityView?.Invoke(EntityListViewModel);
                break;
        }
    }


}