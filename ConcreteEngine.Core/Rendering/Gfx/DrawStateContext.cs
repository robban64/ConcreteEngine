using ConcreteEngine.Core.Rendering.Data;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Core.Rendering.Gfx;

internal sealed class DrawStateContextPayload
{
   public required RenderRegistry Registry { get; init; }
   public required RenderView RenderView { get; init;}
   public required RenderGlobalSnapshot Snapshot { get; init;}
   public required GfxContext Gfx { get; init; }
}

internal sealed class DrawStateContext
{
   public enum StateModeKind
   {
      Main,
      Depth,
      Post
   }
   
   private readonly ShaderId _depthShader;

   public ShaderId OverrideDrawShader { get;  private set; }
   public TextureId DepthTexture { get; private set; }
   public StateModeKind StateMode { get; set; } 


   internal DrawStateContext(ShaderId depthShader, TextureId depthTexture)
   {
      _depthShader = depthShader;
      DepthTexture = depthTexture;
   }

   public void SetDepthMode()
   {
      OverrideDrawShader = _depthShader;
      StateMode = StateModeKind.Depth;

   }

   public void RestoreStateMode()
   {
      OverrideDrawShader = default;
      StateMode = StateModeKind.Main;
   }
}