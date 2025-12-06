namespace ConcreteEngine.Editor.Data;

public enum EditorDataRequestStatus : byte
{
    Ignore,
    Fail,
    Success,
    Overwrite
}

public ref struct EditorDataRequest<T> where T : unmanaged
{
    public ref long Generation;
    public ref T EditorData;
    public bool WriteRequest;
    public EditorDataRequestStatus ResponseStatus = EditorDataRequestStatus.Ignore;

    public EditorDataRequest(ref long generation, ref T editorData, bool writeRequest)
    {
        Generation = ref generation;
        EditorData = ref editorData;
        WriteRequest = writeRequest;
    }

    public bool HasNewData => (!WriteRequest && ResponseStatus == EditorDataRequestStatus.Success) ||
                              ResponseStatus == EditorDataRequestStatus.Overwrite;
}