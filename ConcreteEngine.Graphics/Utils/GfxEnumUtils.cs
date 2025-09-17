using System.Runtime.CompilerServices;

namespace ConcreteEngine.Graphics.Utils;

internal static class GfxEnumUtils
{

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static (bool, uint) ToSamples(this RenderBufferMsaa msaa)
    {
        return msaa switch
        {
            RenderBufferMsaa.None => (false, 0),
            RenderBufferMsaa.X2 => (true, 2u),
            RenderBufferMsaa.X4 => (true, 4u),
            RenderBufferMsaa.X8 => (true, 8u),
            _ => throw new ArgumentOutOfRangeException(nameof(msaa), msaa, null)
        };
    }
    
}