namespace ConcreteEngine.Graphics.Resources;

public readonly struct ResourceIdComparer<TId> : IComparer<TId> where TId : unmanaged,IResourceId 
{
    public int Compare(TId x, TId y)
    {
        return x.Value.CompareTo(y.Value);
    }
}
