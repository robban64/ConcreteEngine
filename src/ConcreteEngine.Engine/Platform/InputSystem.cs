using System.Diagnostics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Engine.Editor;
using Silk.NET.Input;

namespace ConcreteEngine.Engine.Platform;

public sealed class InputSystem : IGameEngineSystem
{
    private readonly IInputContext _context;

    private readonly EngineInputSource _source;

    internal EditorInputControllerImpl EditorController { get; }
    public InputController Controller { get; }

    internal InputSystem(EngineInputSource source)
    {
        _source = source;
        Controller = new InputController(source);
        EditorController = new EditorInputControllerImpl(source);
    }

    public void Initialize()
    {
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void Update()
    {
        _source.Update();
        EditorController.Update();
    }

    internal void ClearInputState() => _source.Clear();

    public void Shutdown()
    {
    }
}