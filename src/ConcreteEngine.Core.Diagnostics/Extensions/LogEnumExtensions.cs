using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Diagnostics.Logging;

namespace ConcreteEngine.Core.Diagnostics.Extensions;

public static class LogEnumExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<byte> ToLogText(this LogLevel value)
    {
        return value switch
        {
            LogLevel.None => "None"u8,
            LogLevel.Trace => "Trace"u8,
            LogLevel.Debug => "Debug"u8,
            LogLevel.Info => "Info"u8,
            LogLevel.Warn => "Warn"u8,
            LogLevel.Error => "Error"u8,
            LogLevel.Critical => "Critical"u8,
            _ => "Unknown"u8
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<byte> ToLogText(this LogScope value)
    {
        return value switch
        {
            LogScope.Unknown => "Unknown"u8,
            LogScope.Engine => "Engine"u8,
            LogScope.Assets => "Asset"u8,
            LogScope.World => "World"u8,
            LogScope.Renderer => "Render"u8,
            LogScope.Gfx => "Graphics"u8,
            LogScope.Backend => "Backend"u8,
            LogScope.Editor => "Editor"u8,
            _ => "Unknown"u8
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