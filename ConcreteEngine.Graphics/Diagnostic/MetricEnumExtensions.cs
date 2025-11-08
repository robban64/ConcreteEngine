#region

using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Shared.Diagnostics;

#endregion

namespace ConcreteEngine.Graphics.Diagnostic;

public static class MetricEnumExtensions
{
    public static LogTopic ToLogTopic(this ResourceKind kind)
    {
        return kind switch
        {
            ResourceKind.Texture => LogTopic.Texture,
            ResourceKind.Shader => LogTopic.Shader,
            ResourceKind.Mesh => LogTopic.Mesh,
            ResourceKind.VertexBuffer => LogTopic.VertexBuffer,
            ResourceKind.IndexBuffer => LogTopic.IndexBuffer,
            ResourceKind.UniformBuffer => LogTopic.UniformBuffer,
            ResourceKind.FrameBuffer => LogTopic.FrameBuffer,
            ResourceKind.RenderBuffer => LogTopic.RenderBuffer,
            ResourceKind.Invalid => throw new ArgumentOutOfRangeException(nameof(kind), "Invalid resource kind"),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };
    }

    public static ResourceKind ToResourceKind(this LogTopic topic)
    {
        return topic switch
        {
            LogTopic.Texture => ResourceKind.Texture,
            LogTopic.Shader => ResourceKind.Shader,
            LogTopic.Mesh => ResourceKind.Mesh,
            LogTopic.VertexBuffer => ResourceKind.VertexBuffer,
            LogTopic.IndexBuffer => ResourceKind.IndexBuffer,
            LogTopic.UniformBuffer => ResourceKind.UniformBuffer,
            LogTopic.FrameBuffer => ResourceKind.FrameBuffer,
            LogTopic.RenderBuffer => ResourceKind.RenderBuffer,
            LogTopic.Unknown => throw new ArgumentOutOfRangeException(nameof(topic), "Unknown log topic"),
            _ => throw new ArgumentOutOfRangeException(nameof(topic), topic, null)
        };
    }

    public static string ToResourceName(this ResourceKind kind)
    {
        return kind switch
        {
            ResourceKind.Texture => "Texture",
            ResourceKind.Shader => "Shader",
            ResourceKind.Mesh => "Mesh",
            ResourceKind.VertexBuffer => "VertexBuffer",
            ResourceKind.IndexBuffer => "IndexBuffer",
            ResourceKind.UniformBuffer => "UniformBuffer",
            ResourceKind.FrameBuffer => "FrameBuffer",
            ResourceKind.RenderBuffer => "RenderBuffer",
            _ => "Invalid"
        };
    }

    public static string ToShortText(this ResourceKind kind)
    {
        return kind switch
        {
            ResourceKind.Texture => "TEX",
            ResourceKind.Shader => "SHD",
            ResourceKind.Mesh => "MSH",
            ResourceKind.VertexBuffer => "VBO",
            ResourceKind.IndexBuffer => "IBO",
            ResourceKind.UniformBuffer => "UBO",
            ResourceKind.FrameBuffer => "FBO",
            ResourceKind.RenderBuffer => "RBO",
            _ => "INV"
        };
    }
}