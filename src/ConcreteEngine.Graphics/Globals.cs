global using ConcreteEngine.Graphics.Gfx.Resources;
global using TextureStore = ConcreteEngine.Graphics.Gfx.Resources.GfxResourceStore<
    ConcreteEngine.Graphics.Gfx.Handles.TextureId,
    ConcreteEngine.Graphics.Gfx.Handles.TextureMeta>;
global using ShaderStore =
    ConcreteEngine.Graphics.Gfx.Resources.GfxResourceStore<ConcreteEngine.Graphics.Gfx.Handles.ShaderId
        ,
        ConcreteEngine.Graphics.Gfx.Handles.ShaderMeta>;
global using MeshStore =
    ConcreteEngine.Graphics.Gfx.Resources.GfxResourceStore<ConcreteEngine.Graphics.Gfx.Handles.MeshId,
        ConcreteEngine.Graphics.Gfx.Handles.MeshMeta>;
global using VboStore = ConcreteEngine.Graphics.Gfx.Resources.GfxResourceStore<
    ConcreteEngine.Graphics.Gfx.Handles.VertexBufferId,
    ConcreteEngine.Graphics.Gfx.Handles.VertexBufferMeta>;
global using IboStore = ConcreteEngine.Graphics.Gfx.Resources.GfxResourceStore<
    ConcreteEngine.Graphics.Gfx.Handles.IndexBufferId,
    ConcreteEngine.Graphics.Gfx.Handles.IndexBufferMeta>;
global using FboStore = ConcreteEngine.Graphics.Gfx.Resources.GfxResourceStore<
    ConcreteEngine.Graphics.Gfx.Handles.FrameBufferId,
    ConcreteEngine.Graphics.Gfx.Handles.FrameBufferMeta>;
global using RboStore = ConcreteEngine.Graphics.Gfx.Resources.GfxResourceStore<
    ConcreteEngine.Graphics.Gfx.Handles.RenderBufferId,
    ConcreteEngine.Graphics.Gfx.Handles.RenderBufferMeta>;
global using UboStore = ConcreteEngine.Graphics.Gfx.Resources.GfxResourceStore<
    ConcreteEngine.Graphics.Gfx.Handles.UniformBufferId,
    ConcreteEngine.Graphics.Gfx.Handles.UniformBufferMeta>;