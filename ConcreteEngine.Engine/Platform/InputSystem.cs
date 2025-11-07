#region

#endregion

namespace ConcreteEngine.Engine.Platform;

public interface IInputSystem : IGameEngineSystem
{
    IEngineInputSource InputSource { get; }
}

public class InputSystem : IInputSystem
{
    public EngineInputSource InputSourceImpl { get; }
    public IEngineInputSource InputSource => InputSourceImpl;

    public InputSystem(EngineInputSource inputSource)
    {
        InputSourceImpl = inputSource;
    }

    public void Initialize()
    {
    }

    public void Update(bool enableInput = true)
    {
        InputSource.Update(enableInput);
    }

    public void Shutdown()
    {
    }
}