using System.Runtime.CompilerServices;
using ConcreteEngine.Editor;
using ConcreteEngine.Engine.Platform;
using Silk.NET.Input;

namespace ConcreteEngine.Engine.Editor;

internal sealed class EditorController(InputSystem input) : EditorEngineController
{
    private readonly InputLayer _layer = input.GetLayer(InputLayerKind.Ui);

    public override void Update()
    {
        Mouse = input.MouseState;
    }

    public override void ToggleBlockInput(bool block) => throw new NotImplementedException();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override ReadOnlySpan<Key> GetActiveKeys() => input.Source.GetActiveKeys();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override ReadOnlySpan<char> GetKeyChars() => input.Source.GetKeyChars();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool IsKeyDown(Key key) => _layer.IsKeyDown(key);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool IsKeyPressed(Key key) => _layer.IsKeyPressed(key);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool IsMouseDown(MouseButton button) => _layer.IsMouseDown(button);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool IsMousePressed(MouseButton button) => _layer.IsMousePressed(button);
}