#region

using System.Numerics;
using ConcreteEngine.Common.Time;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Store;

#endregion

namespace ConcreteEngine.Editor.Data;

public struct EditorSelectionState
{
    public long LastUpdate;
    public EditorId Id;
    public EditorMouseAction Action;
    public bool IsRequesting;
    public bool IsDirty;

    internal void ClearFrame()
    {
        Id = EditorId.Empty;
        Action = EditorMouseAction.None;
        IsRequesting = false;
        IsDirty = false;
    }

    internal void RefreshTime() => LastUpdate = TimeUtils.GetFastTimestamp();
}
