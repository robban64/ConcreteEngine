using ConcreteEngine.Core.Engine.Input;
using ConcreteEngine.Engine.Platform;
using Silk.NET.Input;

namespace ConcreteEngine.Engine.Gateway;

internal sealed class EditorInputController(InputSystem input) : InputController(input.MouseState)
{
    private readonly InputLayer _layer = input.GetLayer(InputLayerKind.Ui);
    private readonly EngineInputSource _source = input.Source;

    public override bool HasEmptyKeyChars => _source.HasEmptyKeyChars;
    public override bool HasEmptyKeyInput => _source.HasEmptyKeyInput;

    public override void ToggleBlockInput(bool block)
    {
        if (block) input.SetActiveLayer(InputLayerKind.Ui);
        else input.ActiveAllLayers();
    }

    public override ReadOnlySpan<Key> GetActiveKeys() => _source.GetActiveKeys();

    public override ReadOnlySpan<char> GetKeyChars() => _source.GetKeyChars();

    public override bool IsKeyDown(Key key) => _layer.IsKeyDown(key);

    public override bool IsKeyPressed(Key key) => _layer.IsKeyPressed(key);

    public override bool IsKeyUp(Key key) => _layer.IsKeyUp(key);

    public override bool IsMouseDown(MouseButton button) => _layer.IsMouseDown(button);

    public override bool IsMousePressed(MouseButton button) => _layer.IsMousePressed(button);
}