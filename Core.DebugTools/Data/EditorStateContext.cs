using Core.DebugTools.Definitions;

namespace Core.DebugTools.Data;

internal sealed class EditorStateContext
{
    public LeftPanelMode LeftMode { get; set; }
    public AssetStoreViewModel AssetViewModel { get; } = new();

    public void ExecuteCommand(string name, string? args1, string? args2)
    {
    }

}