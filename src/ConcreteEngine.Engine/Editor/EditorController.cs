using System.Runtime.CompilerServices;
using ConcreteEngine.Engine.Platform;
using Silk.NET.Input;

namespace ConcreteEngine.Engine.Editor;

internal sealed class InputController(InputSystem input) : ConcreteEngine.Editor.Bridge.InputController
{
    private readonly InputLayer _layer = input.GetLayer(InputLayerKind.Ui);
    private readonly EngineInputSource _source = input.Source;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Update()
    {
        Mouse = input.MouseState;
        HasEmptyKeyInput = _source.HasEmptyKeyInput;
        HasEmptyKeyChars = _source.HasEmptyKeyChars;
    }

    public override void ToggleBlockInput(bool block)
    {
        if (block) input.SetActiveLayer(InputLayerKind.Ui);
        else input.ActiveAllLayers();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override ReadOnlySpan<Key> GetActiveKeys() => _source.GetActiveKeys();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override ReadOnlySpan<char> GetKeyChars() => _source.GetKeyChars();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool IsKeyDown(Key key) => _layer.IsKeyDown(key);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool IsKeyPressed(Key key) => _layer.IsKeyPressed(key);

    public override bool IsKeyUp(Key key) => _layer.IsKeyUp(key);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool IsMouseDown(MouseButton button) => _layer.IsMouseDown(button);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool IsMousePressed(MouseButton button) => _layer.IsMousePressed(button);
}