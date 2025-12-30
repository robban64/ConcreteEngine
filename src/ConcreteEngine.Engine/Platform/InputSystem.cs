using System.Runtime.CompilerServices;

namespace ConcreteEngine.Engine.Platform;

public sealed class InputSystem : IGameEngineSystem
{
    public EngineInputSource InputSource { get; }

    private bool _prevEnabled;
    
    public InputSystem(EngineInputSource inputSource)
    {
        InputSource = inputSource;
    }

    public void Initialize()
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Update(bool enableInput)
    {
        if(!enableInput && !_prevEnabled) return;
        InputSource.Update(enableInput);
        _prevEnabled = enableInput;
    }

    public void Shutdown()
    {
    }
}