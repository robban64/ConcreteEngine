using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Editor.Utils;

namespace ConcreteEngine.Editor;

public static class EditorTime
{
    public static float DeltaTime;
    public static float Fps => DeltaTime / (DeltaTime * DeltaTime + FloatMath.SingularEpsilon);

    private static RefreshRateTicker _rateTicker = RefreshRateTicker.Make();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Advance(float renderDelta) => _rateTicker.Accumulate(renderDelta, out DeltaTime);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WakeUp() => _rateTicker.WakeUp();
}