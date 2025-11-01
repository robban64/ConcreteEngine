#region

global using ConcreteEngine.Graphics.Gfx.Resources;
global using TextureStore =
    ConcreteEngine.Graphics.Gfx.Resources.GfxResourceStore<ConcreteEngine.Graphics.Gfx.Resources.TextureId,
        ConcreteEngine.Graphics.Gfx.Resources.TextureMeta>;
global using ShaderStore =
    ConcreteEngine.Graphics.Gfx.Resources.GfxResourceStore<ConcreteEngine.Graphics.Gfx.Resources.ShaderId,
        ConcreteEngine.Graphics.Gfx.Resources.ShaderMeta>;
global using MeshStore =
    ConcreteEngine.Graphics.Gfx.Resources.GfxResourceStore<ConcreteEngine.Graphics.Gfx.Resources.MeshId,
        ConcreteEngine.Graphics.Gfx.Resources.MeshMeta>;
global using VboStore =
    ConcreteEngine.Graphics.Gfx.Resources.GfxResourceStore<ConcreteEngine.Graphics.Gfx.Resources.VertexBufferId,
        ConcreteEngine.Graphics.Gfx.Resources.VertexBufferMeta>;
global using IboStore =
    ConcreteEngine.Graphics.Gfx.Resources.GfxResourceStore<ConcreteEngine.Graphics.Gfx.Resources.IndexBufferId,
        ConcreteEngine.Graphics.Gfx.Resources.IndexBufferMeta>;
global using FboStore =
    ConcreteEngine.Graphics.Gfx.Resources.GfxResourceStore<ConcreteEngine.Graphics.Gfx.Resources.FrameBufferId,
        ConcreteEngine.Graphics.Gfx.Resources.FrameBufferMeta>;
global using RboStore =
    ConcreteEngine.Graphics.Gfx.Resources.GfxResourceStore<ConcreteEngine.Graphics.Gfx.Resources.RenderBufferId,
        ConcreteEngine.Graphics.Gfx.Resources.RenderBufferMeta>;
global using UboStore =
    ConcreteEngine.Graphics.Gfx.Resources.GfxResourceStore<ConcreteEngine.Graphics.Gfx.Resources.UniformBufferId,
        ConcreteEngine.Graphics.Gfx.Resources.UniformBufferMeta>;

#endregion