namespace ConcreteEngine.Editor.Components.State;

internal sealed class SlotState<T> where T : unmanaged
{
    public T Data;
    public long Generation;
    public SlotView<T> GetView() => new(ref Data, ref Generation);
}

public readonly ref struct SlotView<T>(ref T state, ref long generation) where T : unmanaged
{
    public readonly ref T Data = ref state;
    public readonly ref long Gen = ref generation;
}