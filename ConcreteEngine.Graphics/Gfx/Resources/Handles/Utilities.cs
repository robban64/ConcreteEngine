namespace ConcreteEngine.Graphics.Gfx.Resources;

internal ref struct StoreReturnData<TId, TMeta>
    where TId : unmanaged, IResourceId where TMeta : unmanaged, IResourceMeta
{
    private ref GfxRefToken<TId> _refToken;
    private ref TMeta _meta;

    public StoreReturnData(ref GfxRefToken<TId> handle, ref TMeta header)
    {
        _refToken = ref handle;
        _meta = ref header;
    }

    public ref readonly GfxRefToken<TId> RefToken => ref _refToken;
    public ref readonly TMeta Header => ref _meta;
}

public readonly struct ResourceIdComparer<TId> : IComparer<TId> where TId : unmanaged, IResourceId
{
    public int Compare(TId x, TId y)
    {
        return x.Value.CompareTo(y.Value);
    }
}