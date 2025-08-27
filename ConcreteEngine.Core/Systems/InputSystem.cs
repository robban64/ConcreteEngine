using ConcreteEngine.Core.Platform;

namespace ConcreteEngine.Core.Systems;


public class InputSystem : IGameEngineSystem
{
    private IEngineInputSource _inputSource;
    
    public IEngineInputSource InputSource => _inputSource;

    public InputSystem(IEngineInputSource inputSource)
    {
        _inputSource = inputSource;
    }
    
    public void Initialize()
    {
    }

    public void Shutdown()
    {
    }
}