using System.Diagnostics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Diagnostics.Time;
using Silk.NET.Input;

namespace ConcreteEngine.Engine.Platform;

public sealed class InputSystem : IGameEngineSystem
{
    private readonly IInputContext _context;
    
    private readonly EngineInputSource _source;
    public InputController Controller { get; }
    
    internal InputSystem(EngineInputSource source)
    {
        _source = source;
        Controller = new InputController(source);
    }

    public void Initialize()
    {
    }
    
    
    internal void Update(float dt)
    {
        _source.Update(dt);
    }

    internal void ClearInputState() => _source.Clear();

    public void Shutdown()
    {
    }
}