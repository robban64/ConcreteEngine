using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Graphics.OpenGL;

internal readonly record struct GlFboHandleMeta(GlFboHandle Handle, in FrameBufferMeta Meta);
internal readonly record struct GlTexHandleMeta(GlTextureHandle Handle, in TextureMeta Meta) ;
internal readonly record struct GlRboHandleMeta(GlRboHandle Handle, in RenderBufferMeta Meta);
