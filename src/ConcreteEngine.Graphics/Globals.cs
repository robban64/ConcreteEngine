global using FboStore =
    ConcreteEngine.Graphics.Resources.GfxResourceStore<ConcreteEngine.Graphics.Handles.FrameBufferMeta>;
global using IboStore =
    ConcreteEngine.Graphics.Resources.GfxResourceStore<ConcreteEngine.Graphics.Handles.IndexBufferMeta>;
global using MeshStore = ConcreteEngine.Graphics.Resources.GfxResourceStore<ConcreteEngine.Graphics.Handles.MeshMeta>;
global using RboStore =
    ConcreteEngine.Graphics.Resources.GfxResourceStore<ConcreteEngine.Graphics.Handles.RenderBufferMeta>;
global using ShaderStore =
    ConcreteEngine.Graphics.Resources.GfxResourceStore<ConcreteEngine.Graphics.Handles.ShaderMeta>;
global using TextureStore =
    ConcreteEngine.Graphics.Resources.GfxResourceStore<ConcreteEngine.Graphics.Handles.TextureMeta>;
global using UboStore =
    ConcreteEngine.Graphics.Resources.GfxResourceStore<ConcreteEngine.Graphics.Handles.UniformBufferMeta>;
global using VboStore =
    ConcreteEngine.Graphics.Resources.GfxResourceStore<ConcreteEngine.Graphics.Handles.VertexBufferMeta>;
global using TextureId = ConcreteEngine.Graphics.Handles.GfxId<ConcreteEngine.Graphics.Handles.TextureMeta>;
global using ShaderId = ConcreteEngine.Graphics.Handles.GfxId<ConcreteEngine.Graphics.Handles.ShaderMeta>;
global using MeshId = ConcreteEngine.Graphics.Handles.GfxId<ConcreteEngine.Graphics.Handles.MeshMeta>;
global using VertexBufferId = ConcreteEngine.Graphics.Handles.GfxId<ConcreteEngine.Graphics.Handles.VertexBufferMeta>;
global using IndexBufferId = ConcreteEngine.Graphics.Handles.GfxId<ConcreteEngine.Graphics.Handles.IndexBufferMeta>;
global using FrameBufferId = ConcreteEngine.Graphics.Handles.GfxId<ConcreteEngine.Graphics.Handles.FrameBufferMeta>;
global using RenderBufferId = ConcreteEngine.Graphics.Handles.GfxId<ConcreteEngine.Graphics.Handles.RenderBufferMeta>;
global using UniformBufferId = ConcreteEngine.Graphics.Handles.GfxId<ConcreteEngine.Graphics.Handles.UniformBufferMeta>;