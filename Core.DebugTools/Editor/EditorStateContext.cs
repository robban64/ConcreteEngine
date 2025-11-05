using System.Numerics;
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
        //if (mode != SidebarEditorMode.Entities) EntityListViewModel.ResetState();

        switch (mode)
        {
            case SidebarEditorMode.Assets:
                EditorTable.FillAssetStoreView?.Invoke(AssetViewModel.TypeSelection, AssetViewModel.AssetObjects);
                break;
            case SidebarEditorMode.Entities:
                if(EntityListViewModel.Entities.Count == 0)
                    EditorTable.FillEntityView?.Invoke(EntityListViewModel);
                break;
        }
    }


    private bool CanExecute(int ms, bool print = false)
    {
        if (!TimeUtils.HasIntervalPassed(_lastAction, ms))
        {
            _devConsoleService.AddLog("Command delay time has not passed");
            return false;
        }

        _lastAction = TimeUtils.GetTimestamp();
        return true;
    }

    public void ExecuteReloadShader(AssetObjectViewModel viewModel)
    {
        if (!CanExecute(1000, true)) return;
        _devConsoleService.ExecuteInternalCommand(CoreCmdNames.AssetShader, "reload", viewModel.Name);
    }

    public void ExecuteSetEntityTransform(EntityViewModel entity)
    {
        //if (!CanExecute(25)) return;
        var payload = new TransformCmdPayload(entity.EntityId, in entity.Transform);
        var req = new ConsoleCommandRequest(CoreCmdNames.EntityTransform, "set", Payload: payload);
        _devConsoleService.ExecuteInternalCommand(req);
    }
}