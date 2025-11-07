using ConcreteEngine.Common.Diagnostics;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Gui;
using ConcreteEngine.Editor.Utils;

namespace ConcreteEngine.Editor;

internal sealed class EditorStateContext
{
    private readonly DevConsoleService _devConsoleService;

    public EditorViewMode ViewMode { get; private set; } = EditorViewMode.None;
    public LeftSidebarMode LeftSidebarMode { get; private set; } = LeftSidebarMode.None;
    public RightSidebarMode PropertyMode { get; private set; } = RightSidebarMode.None;

    public AssetStoreViewModel AssetViewModel { get; } = new();
    public EntityListViewModel EntityListViewModel { get; } = new();

    public CameraViewModel CameraModel { get; } = new();
    
    private long _lastAction = TimeUtils.GetTimestamp();

    public EditorStateContext(DevConsoleService devConsoleService)
    {
        _devConsoleService = devConsoleService;
    }

    internal void PreRender()
    {
        GuiTheme.RightSidebarExpanded = ViewMode == EditorViewMode.Editor;
    }

    public void SetViewMode(EditorViewMode mode)
    {
        if (mode == ViewMode) return;
        ViewMode = mode;

        MetricsApi.ToggleMetrics(ViewMode == EditorViewMode.Metrics);
    }

    public void SetSidebarMode(LeftSidebarMode mode)
    {
        if (mode == LeftSidebarMode) return;
        LeftSidebarMode = mode;

        if (mode != LeftSidebarMode.Assets) AssetViewModel.ResetState();
        //if (mode != SidebarEditorMode.Entities) EntityListViewModel.ResetState();

        switch (mode)
        {
            case LeftSidebarMode.Assets:
                EditorApi.FillAssetStoreView?.Invoke(AssetViewModel.TypeSelection, AssetViewModel.AssetObjects);
                break;
            case LeftSidebarMode.Entities:
                if (EntityListViewModel.Entities.Count == 0)
                    EditorApi.FillEntityView?.Invoke(EntityListViewModel);
                break;
        }
    }

    public void SetPropertyMode(RightSidebarMode mode)
    {
        if (mode == PropertyMode) return;
        PropertyMode = mode;

        switch (mode)
        {
            case RightSidebarMode.None: break;
            case RightSidebarMode.Camera:
                RefreshCameraData();
                break;
            case RightSidebarMode.Light: break;
            case RightSidebarMode.Sky: break;
            case RightSidebarMode.Terrain: break;
        }
    }

    public void RefreshCameraData()
    {
        if(!EditorApi.FetchCameraData(CameraModel.Generation, out var response))
            return;
        
        CameraModel.FromDataModel(in response);
        CameraPropertyGui.UpdateStateFromViewModel();
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
    
    public void ExecuteSetCameraTransform(in CameraEditorPayload payload)
    {
        //if (!CanExecute(25)) return;
        CommandDispatcher.InvokeEditorCommand(CoreCmdNames.CameraTransform, in payload);
    }

}