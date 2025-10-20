#region

#endregion

namespace ConcreteEngine.Core.Platform;

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

    public void Update(bool enableInput = true)
    {
        _inputSource.Update(enableInput);
    }

    public void Shutdown()
    {
    }
}