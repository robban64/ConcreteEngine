global using FboStore = ConcreteEngine.Graphics.Resources.GfxResourceStore<
    ConcreteEngine.Graphics.Handles.FrameBufferId,
    ConcreteEngine.Graphics.Handles.FrameBufferMeta>;
global using IboStore = ConcreteEngine.Graphics.Resources.GfxResourceStore<
    ConcreteEngine.Graphics.Handles.IndexBufferId,
    ConcreteEngine.Graphics.Handles.IndexBufferMeta>;
global using MeshStore = ConcreteEngine.Graphics.Resources.GfxResourceStore<ConcreteEngine.Graphics.Handles.MeshId,
    ConcreteEngine.Graphics.Handles.MeshMeta>;
global using RboStore = ConcreteEngine.Graphics.Resources.GfxResourceStore<
    ConcreteEngine.Graphics.Handles.RenderBufferId,
    ConcreteEngine.Graphics.Handles.RenderBufferMeta>;
global using ShaderStore = ConcreteEngine.Graphics.Resources.GfxResourceStore<ConcreteEngine.Graphics.Handles.ShaderId
    ,
    ConcreteEngine.Graphics.Handles.ShaderMeta>;
global using TextureStore = ConcreteEngine.Graphics.Resources.GfxResourceStore<
    ConcreteEngine.Graphics.Handles.TextureId,
    ConcreteEngine.Graphics.Handles.TextureMeta>;
global using UboStore = ConcreteEngine.Graphics.Resources.GfxResourceStore<
    ConcreteEngine.Graphics.Handles.UniformBufferId,
    ConcreteEngine.Graphics.Handles.UniformBufferMeta>;
global using VboStore = ConcreteEngine.Graphics.Resources.GfxResourceStore<
    ConcreteEngine.Graphics.Handles.VertexBufferId,
    ConcreteEngine.Graphics.Handles.VertexBufferMeta>;