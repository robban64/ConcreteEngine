using System.Runtime.CompilerServices;

namespace ConcreteEngine.Editor;

public static class EditorTime
{
    public static float DeltaTime;
    
    private static RefreshRateTicker _rateTicker = RefreshRateTicker.Make();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Advance(float renderDelta) => _rateTicker.Accumulate(renderDelta, out DeltaTime);
    
    public static void WakeUp() => _rateTicker.WakeUp();
}