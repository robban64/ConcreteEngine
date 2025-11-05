using Core.DebugTools.Data;

namespace Core.DebugTools.Editor;

internal static class EditorActions
{
    public static Func<string, string?, string?, bool>? ExecuteCommand { get; set; }
    
    public static bool ExecuteReloadShader(AssetObjectViewModel viewModel)
    {
        return ExecuteCommand!(CoreCmdNames.ShaderReload, viewModel.Name, null);
    }

}