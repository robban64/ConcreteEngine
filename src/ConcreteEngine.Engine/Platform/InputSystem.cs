namespace ConcreteEngine.Engine.Platform;

public sealed class InputSystem : IGameEngineSystem
{
    public EngineInputSource InputSource { get; }

    public InputSystem(EngineInputSource inputSource)
    {
        InputSource = inputSource;
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