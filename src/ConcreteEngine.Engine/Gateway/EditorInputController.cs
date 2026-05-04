using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Engine.Input;
using ConcreteEngine.Engine.Platform;
using Silk.NET.Input;

namespace ConcreteEngine.Engine.Gateway;

internal sealed class EditorInputController : InputController
{
    private readonly InputLayer _layer;
    private readonly EngineInputSource _source;
    private readonly InputSystem _input;

    public EditorInputController(InputSystem input)
    {
        _input = input;
        _layer = input.GetLayer(InputLayerKind.Ui);
        _source = input.Source;
        Mouse = input.MouseState;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Update()
    {
        HasEmptyKeyInput = _source.HasEmptyKeyInput;
        HasEmptyKeyChars = _source.HasEmptyKeyChars;
    }

    public override void ToggleBlockInput(bool block)
    {
        if (block) _input.SetActiveLayer(InputLayerKind.Ui);
        else _input.ActiveAllLayers();
    }

    public override ReadOnlySpan<Key> GetActiveKeys() => _source.GetActiveKeys();

    public override ReadOnlySpan<char> GetKeyChars() => _source.GetKeyChars();

    public override bool IsKeyDown(Key key) => _layer.IsKeyDown(key);

    public override bool IsKeyPressed(Key key) => _layer.IsKeyPressed(key);

    public override bool IsKeyUp(Key key) => _layer.IsKeyUp(key);

    public override bool IsMouseDown(MouseButton button) => _layer.IsMouseDown(button);

    public override bool IsMousePressed(MouseButton button) => _layer.IsMousePressed(button);
}