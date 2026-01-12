using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Graphics.Gfx;

namespace ConcreteEngine.Graphics.Diagnostic;

public static class MetricEnumExtensions
{
    public static LogTopic ToLogTopic(this GraphicsKind kind)
    {
        return kind switch
        {
            GraphicsKind.Texture => LogTopic.Texture,
            GraphicsKind.Shader => LogTopic.Shader,
            GraphicsKind.Mesh => LogTopic.Mesh,
            GraphicsKind.VertexBuffer => LogTopic.VertexBuffer,
            GraphicsKind.IndexBuffer => LogTopic.IndexBuffer,
            GraphicsKind.UniformBuffer => LogTopic.UniformBuffer,
            GraphicsKind.FrameBuffer => LogTopic.FrameBuffer,
            GraphicsKind.RenderBuffer => LogTopic.RenderBuffer,
            GraphicsKind.Invalid => LogTopic.Unknown,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };
    }

    public static GraphicsKind ToResourceKind(this LogTopic topic)
    {
        return topic switch
        {
            LogTopic.Texture => GraphicsKind.Texture,
            LogTopic.Shader => GraphicsKind.Shader,
            LogTopic.Mesh => GraphicsKind.Mesh,
            LogTopic.VertexBuffer => GraphicsKind.VertexBuffer,
            LogTopic.IndexBuffer => GraphicsKind.IndexBuffer,
            LogTopic.UniformBuffer => GraphicsKind.UniformBuffer,
            LogTopic.FrameBuffer => GraphicsKind.FrameBuffer,
            LogTopic.RenderBuffer => GraphicsKind.RenderBuffer,
            LogTopic.Unknown => GraphicsKind.Invalid,
            _ => throw new ArgumentOutOfRangeException(nameof(topic), topic, null)
        };
    }
}