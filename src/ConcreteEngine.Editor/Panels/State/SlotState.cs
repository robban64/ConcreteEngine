namespace ConcreteEngine.Editor.Panels.State;

public sealed class SlotState<T> where T : unmanaged
{
    public T Data;
    public long Generation;
}
