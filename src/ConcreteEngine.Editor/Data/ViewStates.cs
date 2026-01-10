namespace ConcreteEngine.Editor.Data;

internal sealed class SlotState<T> where  T :unmanaged
{
    public T State;
    public long Generation;
    public EditorSlot<T> GetView() => new(ref State, ref Generation);
}
