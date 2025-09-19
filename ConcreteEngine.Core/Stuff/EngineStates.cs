namespace ConcreteEngine.Core.Systems;
/*
internal sealed class EngineNoopState : BaseEngineState
{
    protected override void OnEnter()
    {
    }
    public override bool ShouldAdvance() => true;
}

internal enum EngineCoreEvent
{
    AssetsLoaded,
    SceneTransitioned,
}

internal enum EngineCoreCommand 
{
    LoadAssets,
    LoadFirstScene,
}

internal enum EngineCoreUpdateMode
{
    Managed,
    Unmanaged
}

internal enum EngineCoreRenderMOde
{
    
}

internal sealed class EngineCoreContext
{
    private readonly GameEngine _engine;


    public EngineCoreContext(GameEngine engine)
    {
        _engine = engine;
    }

    public void Notify(EngineCoreEvent evt) => _engine.OnNotify(evt);
    public void ExecuteCmd(EngineCoreCommand cmd) => _engine.OnExecuteCmd(cmd);
    
    public bool ProcessAssets(int n) => _engine.ProcessAssets(n);

}


internal class EngineLoaderState : BaseEngineState
{
    private int _state = 0;
    private const int StateMax = 4;

    public override void OnUpdate(in FrameMetaInfo frameCtx)
    {
        switch (_state)
        {
            case 3:
                Context.ExecuteCmd(EngineCoreCommand.LoadFirstScene);
                _state = 4;
                break;
        }
    }

    public override void OnRender(in FrameMetaInfo frameCtx)
    {
        switch (_state)
        {
            case 0:
                Context.ExecuteCmd(EngineCoreCommand.LoadAssets);
                _state = 1;
                break;
            case 1:
                if(Context.ProcessAssets(4)) _state = 2;
                break;
            case 2:
                Context.Notify(EngineCoreEvent.AssetsLoaded);
                _state = 3;
                break;
        }
        
    }

    protected override void OnExit() {}

    public override bool ShouldAdvance() => _state >= StateMax;
    
}

internal class EngineSceneState : BaseEngineState
{
    
    protected override void OnEnter()
    {
    }

    public override void OnRender(in FrameMetaInfo frameCtx)
    {
    }

    public override bool ShouldAdvance() => _done;
    
}*/