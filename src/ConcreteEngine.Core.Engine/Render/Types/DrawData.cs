using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;

namespace ConcreteEngine.Core.Engine.Render;

internal sealed class DrawData
{
    public long FrameVersion { get; private set; }
    public int VisibleCount { get; private set; }
    
    public BitSet VisibleSet { get; private set; }
    
    private NativeArray<ulong> _sortKeys;
}