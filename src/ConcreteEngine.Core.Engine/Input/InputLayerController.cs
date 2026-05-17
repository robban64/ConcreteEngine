using System.Runtime.CompilerServices;
using Silk.NET.Input;

namespace ConcreteEngine.Core.Engine.Input;

internal sealed class InputLayerController(InputSystem input, InputLayerKind layerKind)
    : InputController(input.MouseState)
{
    private readonly InputLayer _layer = input.GetLayer(layerKind);
    private readonly EngineInputSource _source = input.Source;

    public override bool HasEmptyKeyChars => _source.HasEmptyKeyChars;
    public override bool HasEmptyKeyInput => _source.HasEmptyKeyInput;

    public override void ToggleBlockInput(bool block)
    {
        if (block) input.SetActiveLayer(layerKind);
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool IsKeyUp(Key key) => _layer.IsKeyUp(key);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool IsMouseDown(MouseButton button) => _layer.IsMouseDown(button);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool IsMousePressed(MouseButton button) => _layer.IsMousePressed(button);
}