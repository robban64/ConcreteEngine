using ConcreteEngine.Core.Platform;

namespace ConcreteEngine.Core.Systems;

public interface IInputSystem : IGameEngineSystem
{
    IEngineInputSource InputSource { get; }
}

public class InputSystem : IInputSystem
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

    public void Update()
    {
        _inputSource.Update();
    }

    public void Shutdown()
    {
    }
}