using ConcreteEngine.Common.Diagnostics;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Definitions;

namespace ConcreteEngine.Editor;

internal sealed class EditorStateContext
{
    public EditorViewMode ViewMode { get; private set; } = EditorViewMode.None;
    public SidebarEditorMode SidebarMode { get; private set; } = SidebarEditorMode.None;

    public AssetStoreViewModel AssetViewModel { get; } = new();
    public EntityListViewModel EntityListViewModel { get; } = new();

    private readonly DevConsoleService _devConsoleService;

    private long _lastAction = TimeUtils.GetTimestamp();

    public EditorStateContext(DevConsoleService devConsoleService)
    {
        _devConsoleService = devConsoleService;
    }

    public void SetViewMode(EditorViewMode mode)
    {
        if (mode == ViewMode) return;
        ViewMode = mode;

        MetricsApi.ToggleMetrics(ViewMode == EditorViewMode.Metrics);
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
                EditorApi.FillAssetStoreView?.Invoke(AssetViewModel.TypeSelection, AssetViewModel.AssetObjects);
                break;
            case SidebarEditorMode.Entities:
                if (EntityListViewModel.Entities.Count == 0)
                    EditorApi.FillEntityView?.Invoke(EntityListViewModel);
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
        CommandDispatcher.InvokeEditorCommand(CoreCmdNames.AssetShader,
            new EditorShaderPayload(viewModel.Name, EditorRequestAction.Reload));
    }

    public void ExecuteSetEntityTransform(EntityViewModel entity)
    {
        //if (!CanExecute(25)) return;
        var payload = new EditorTransformPayload(entity.EntityId, in entity.Transform);
        CommandDispatcher.InvokeEditorCommand(CoreCmdNames.EntityTransform, in payload);
    }
}