using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Diagnostics.Logging;

namespace ConcreteEngine.Graphics.Diagnostic;

public static class MetricEnumExtensions
{
    public static LogTopic ToLogTopic(this GraphicsKind kind)
    {
        return kind switch
        {
            GraphicsKind.Invalid => LogTopic.Unknown,
            GraphicsKind.Texture => LogTopic.Texture,
            GraphicsKind.Shader => LogTopic.Shader,
            GraphicsKind.Mesh => LogTopic.Mesh,
            GraphicsKind.VertexBuffer => LogTopic.VertexBuffer,
            GraphicsKind.IndexBuffer => LogTopic.IndexBuffer,
            GraphicsKind.UniformBuffer => LogTopic.UniformBuffer,
            GraphicsKind.FrameBuffer => LogTopic.FrameBuffer,
            GraphicsKind.RenderBuffer => LogTopic.RenderBuffer,
            _ => Throwers.Unreachable<LogTopic>(nameof(kind))
        };
    }

    public static GraphicsKind ToResourceKind(this LogTopic topic)
    {
        return topic switch
        {
            LogTopic.Unknown => GraphicsKind.Invalid,
            LogTopic.Texture => GraphicsKind.Texture,
            LogTopic.Shader => GraphicsKind.Shader,
            LogTopic.Mesh => GraphicsKind.Mesh,
            LogTopic.VertexBuffer => GraphicsKind.VertexBuffer,
            LogTopic.IndexBuffer => GraphicsKind.IndexBuffer,
            LogTopic.UniformBuffer => GraphicsKind.UniformBuffer,
            LogTopic.FrameBuffer => GraphicsKind.FrameBuffer,
            LogTopic.RenderBuffer => GraphicsKind.RenderBuffer,
            _ => Throwers.Unreachable<GraphicsKind>(nameof(topic))
        };
    }
}