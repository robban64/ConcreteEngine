namespace ConcreteEngine.Graphics.Gfx.Builders;

internal sealed class GfxBuilders
{
    
    internal GfxBuilders(Context ctx)
    {
        
    }
    
    
    internal sealed class Context
    {
        public GfxBuffers Buffers { get; init; }

        public GfxMeshes Meshes { get; init;}

        public GfxShaders Shaders { get;init; }

        public GfxTextures Textures { get; init;}

        public GfxFrameBuffers FrameBuffers { get;init; } 
    }
}

