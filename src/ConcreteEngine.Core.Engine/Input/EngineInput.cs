using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Numerics;
using Silk.NET.Input;

namespace ConcreteEngine.Core.Engine.Input;

public static  partial class EngineInput
{
    private static readonly List<InputLayer> Layers =
    [
        new(InputLayerKind.Ui) { Enabled = false },
        new(InputLayerKind.Game) { Enabled = true }
    ];

    private static IKeyboard? _keyboardSource;
    private static IMouse? _mouseSource;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static InputLayer GetLayer(InputLayerKind kind) => Layers[(int)kind];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ActiveAllLayers()
    {
        foreach (var layer in Layers) layer.Enabled = true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetActiveLayer(InputLayerKind kind)
    {
        foreach (var layer in Layers) layer.Enabled = kind == layer.Kind;
    }

    internal static void Update()
    {
        Keyboard.UpdateKeys();
        Mouse.UpdateMouse();
    }


    internal static void Attach(IInputContext input)
    {
        if(_keyboardSource  is not null || _mouseSource is not null)
            throw new InvalidOperationException("EngineInput already attached");
        
        _keyboardSource = input.Keyboards[0];
        _mouseSource = input.Mice[0];
        
        Keyboard.Attach(_keyboardSource);
        Mouse.Attach(_mouseSource);
    }

    internal static void Detach()
    {
        if (_keyboardSource is not null)
            Keyboard.Detach(_keyboardSource);

        if (_mouseSource is not null)
            Mouse.Detach(_mouseSource);
    }
}