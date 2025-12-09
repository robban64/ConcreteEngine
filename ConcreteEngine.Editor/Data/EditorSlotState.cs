namespace ConcreteEngine.Editor.Data;

public struct EditorSlotState
{
    public long Generation;
    public int RequestInFrames;
    public bool IsDirty;
    public bool IsRequesting;

    public void RequestData() => IsRequesting = true;

    public void Reset(long generation)
    {
        Generation = generation;
        IsDirty = false;
        IsRequesting = false;
    }
}