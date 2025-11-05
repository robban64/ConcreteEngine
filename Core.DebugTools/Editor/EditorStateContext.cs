using ConcreteEngine.Common.Diagnostics;
using Core.DebugTools.Data;
using Core.DebugTools.Definitions;

namespace Core.DebugTools.Editor;

internal sealed class EditorStateContext
{
    public EditorViewMode ViewMode { get; private set; } = EditorViewMode.None;
    public SidebarEditorMode SidebarMode { get; private set; } = SidebarEditorMode.None;

    public AssetStoreViewModel AssetViewModel { get; } = new();
    public EntityListViewModel EntityListViewModel { get; } = new();

    private readonly MetricService _metricService;
    private readonly DevConsoleService _devConsoleService;
    
    private long _lastAction = TimeUtils.GetTimestamp();

    public EditorStateContext(MetricService metricService, DevConsoleService devConsoleService)
    {
        _metricService = metricService;
        _devConsoleService = devConsoleService;
    }

    public void SetViewMode(EditorViewMode mode)
    {
        if (mode == ViewMode) return;
        ViewMode = mode;

        _metricService.ToogleMetrics(ViewMode == EditorViewMode.Metrics);
    }

    public void SetSidebarMode(SidebarEditorMode mode)
    {
        if (mode == SidebarMode) return;
        SidebarMode = mode;

        if (mode != SidebarEditorMode.Assets) AssetViewModel.ResetState();
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

    
    private bool CanExecute()
    {
        if (!TimeUtils.HasIntervalPassed(_lastAction,4000))
        {
            _devConsoleService.AddLog("Command delay time has not passed");
            return  false;
        }
        _lastAction = TimeUtils.GetTimestamp();
        return true;
    }

    public void ExecuteReloadShader(AssetObjectViewModel viewModel)
    {
        if(!CanExecute())  return;
        _devConsoleService.ExecuteInternalCommand(CoreCmdNames.ShaderReload, viewModel.Name);
    }
    
    public void ExecuteSetEntityTransform(AssetObjectViewModel viewModel)
    {
        if(!CanExecute())  return;
        _devConsoleService.ExecuteInternalCommand(CoreCmdNames.ShaderReload, viewModel.Name);
    }

}