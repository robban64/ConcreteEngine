using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.App.Shared;

internal ref struct ClipperEnumerator
{
    private ref ImGuiListClipper _clipper;

    private ClipperEnumerator(ref ImGuiListClipper clipper) => _clipper = ref clipper;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool MoveNext() => _clipper.Step();

    public Range32 Current
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(_clipper.DisplayStart, _clipper.DisplayEnd - _clipper.DisplayStart);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ClipperEnumerator GetEnumerator() => this;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose() => _clipper.End();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ClipperEnumerator New(ref ImGuiListClipper clipper) => new(ref clipper);
}