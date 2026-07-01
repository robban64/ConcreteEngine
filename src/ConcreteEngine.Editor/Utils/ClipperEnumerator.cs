using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Utils;

internal ref struct ClipperEnumerator
{
    private ImGuiListClipperPtr _clipper;

    private ClipperEnumerator(ImGuiListClipperPtr clipper) => _clipper = clipper;

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
    public static ClipperEnumerator New(ImGuiListClipperPtr clipper) => new(clipper);
}