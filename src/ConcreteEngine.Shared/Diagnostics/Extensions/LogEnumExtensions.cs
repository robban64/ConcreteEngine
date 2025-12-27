using System.Runtime.CompilerServices;

namespace ConcreteEngine.Shared.Diagnostics.Extensions;

public static class LogEnumExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ToLogText(this LogLevel value)
    {
        return value switch
        {
            LogLevel.None => "None",
            LogLevel.Trace => "Trace",
            LogLevel.Debug => "Debug",
            LogLevel.Info => "Info",
            LogLevel.Warn => "Warn",
            LogLevel.Error => "Error",
            LogLevel.Critical => "Critical",
            _ => "Unknown"
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ToLogText(this LogTopic value)
    {
        return value switch
        {
            LogTopic.Unknown => "Unknown",
            LogTopic.Texture => "Texture",
            LogTopic.Shader => "Shader",
            LogTopic.Mesh => "Mesh",
            LogTopic.VertexBuffer => "VertexBuf",
            LogTopic.IndexBuffer => "IndexBuf",
            LogTopic.UniformBuffer => "UniformBuf",
            LogTopic.FrameBuffer => "FrameBuf",
            LogTopic.RenderBuffer => "RenderBuf",
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
    public static string ToLogText(this LogScope value)
    {
        return value switch
        {
            LogScope.Unknown => "Unknown",
            LogScope.Engine => "Engine",
            LogScope.Assets => "Assets",
            LogScope.World => "World",
            LogScope.Backend => "Backend",
            LogScope.Gfx => "Gfx",
            LogScope.Renderer => "Renderer",
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
            _ => "Unknown"
        };
    }
}