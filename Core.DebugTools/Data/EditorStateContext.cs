using Core.DebugTools.Definitions;

namespace Core.DebugTools.Data;

internal sealed class EditorStateContext
{
    public LeftPanelMode LeftMode { get; set; }

    public AssetStoreViewModel AssetViewModel { get; } = new();
}