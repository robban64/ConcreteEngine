namespace ConcreteEngine.Editor.Data;

public sealed class SlotState<T> where T : unmanaged
{
    public T Data;
    public long Generation;
}