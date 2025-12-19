global using ConcreteEngine.Graphics.Gfx.Resources;
global using TextureStore = ConcreteEngine.Graphics.Gfx.Resources.Stores.GfxResourceStore<ConcreteEngine.Graphics.Gfx.Resources.Handles.TextureId,
        ConcreteEngine.Graphics.Gfx.Resources.Handles.TextureMeta>;
global using ShaderStore = ConcreteEngine.Graphics.Gfx.Resources.Stores.GfxResourceStore<ConcreteEngine.Graphics.Gfx.Resources.Handles.ShaderId,
        ConcreteEngine.Graphics.Gfx.Resources.Handles.ShaderMeta>;
global using MeshStore = ConcreteEngine.Graphics.Gfx.Resources.Stores.GfxResourceStore<ConcreteEngine.Graphics.Gfx.Resources.Handles.MeshId,
        ConcreteEngine.Graphics.Gfx.Resources.Handles.MeshMeta>;
global using VboStore = ConcreteEngine.Graphics.Gfx.Resources.Stores.GfxResourceStore<ConcreteEngine.Graphics.Gfx.Resources.Handles.VertexBufferId,
        ConcreteEngine.Graphics.Gfx.Resources.Handles.VertexBufferMeta>;
global using IboStore = ConcreteEngine.Graphics.Gfx.Resources.Stores.GfxResourceStore<ConcreteEngine.Graphics.Gfx.Resources.Handles.IndexBufferId,
        ConcreteEngine.Graphics.Gfx.Resources.Handles.IndexBufferMeta>;
global using FboStore = ConcreteEngine.Graphics.Gfx.Resources.Stores.GfxResourceStore<ConcreteEngine.Graphics.Gfx.Resources.Handles.FrameBufferId,
        ConcreteEngine.Graphics.Gfx.Resources.Handles.FrameBufferMeta>;
global using RboStore = ConcreteEngine.Graphics.Gfx.Resources.Stores.GfxResourceStore<ConcreteEngine.Graphics.Gfx.Resources.Handles.RenderBufferId,
        ConcreteEngine.Graphics.Gfx.Resources.Handles.RenderBufferMeta>;
global using UboStore = ConcreteEngine.Graphics.Gfx.Resources.Stores.GfxResourceStore<ConcreteEngine.Graphics.Gfx.Resources.Handles.UniformBufferId,
        ConcreteEngine.Graphics.Gfx.Resources.Handles.UniformBufferMeta>;