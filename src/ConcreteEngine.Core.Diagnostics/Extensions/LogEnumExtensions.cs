using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Diagnostics.Logging;

namespace ConcreteEngine.Core.Diagnostics.Extensions;

public static class LogEnumExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ToLogText(this LogTopic value)
    {
        return value switch
        {
            LogTopic.Unknown => "Unknown",
            LogTopic.Texture => "TEX",
            LogTopic.Shader => "SHD",
            LogTopic.Mesh => "MESH",
            LogTopic.VertexBuffer => "VBO",
            LogTopic.IndexBuffer => "IBO",
            LogTopic.UniformBuffer => "UBO",
            LogTopic.FrameBuffer => "FBO",
            LogTopic.RenderBuffer => "RBO",
            LogTopic.Material => "Material",
            LogTopic.Io => "AssetIO",
            LogTopic.Renderer => "Renderer",
            LogTopic.Frame => "Frame",
            LogTopic.Pass => "Pass",
            LogTopic.CommandList => "CommandList",
            LogTopic.Pipeline => "Pipeline",
            LogTopic.ArrayBuffer => "ArrayBuffer",
            _ => "Unknown"
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ToLogText(this LogAction value)
    {
        return value switch
        {
            LogAction.Unknown => "Unknown",
            LogAction.Load => "Load",
            LogAction.Unload => "Unload",
            LogAction.Add => "Add",
            LogAction.Remove => "Remove",
            LogAction.Replace => "Replace",
            LogAction.Create => "Create",
            LogAction.Destroy => "Destroy",
            LogAction.Bind => "Bind",
            LogAction.Unbind => "Unbind",
            LogAction.Upload => "Upload",
            LogAction.Download => "Download",
            LogAction.Map => "Map",
            LogAction.Unmap => "Unmap",
            LogAction.Compile => "Compile",
            LogAction.Link => "Link",
            LogAction.Begin => "Begin",
            LogAction.End => "End",
            LogAction.Submit => "Submit",
            LogAction.Resize => "Resize",
            LogAction.Evict => "Evict",
            LogAction.Execute => "Execute",
            LogAction.EnqRemove => "EnqRemove",
            LogAction.EnqReplace => "EnqReplace",
            _ => "Unknown",
        };
    }
}