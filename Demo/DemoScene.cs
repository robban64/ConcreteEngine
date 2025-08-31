#region

using System.Drawing;
using System.Numerics;
using ConcreteEngine.Core.Assets;
using ConcreteEngine.Core.Configuration;
using ConcreteEngine.Core.Features;
using ConcreteEngine.Core.Rendering;
using ConcreteEngine.Core.Rendering.Emitters;
using ConcreteEngine.Core.Rendering.Pipeline;
using ConcreteEngine.Core.Rendering.Renderers;
using ConcreteEngine.Core.Scene;
using ConcreteEngine.Core.Scene.Nodes;
using ConcreteEngine.Core.Transforms;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Data;
using ConcreteEngine.Graphics.Definitions;
using Silk.NET.Maths;
using LightEntity = ConcreteEngine.Core.Scene.LightEntity;
using Shader = ConcreteEngine.Core.Resources.Shader;

#endregion

namespace Demo;

public sealed class DemoScene : GameScene
{
    public override void Initialize()
    {
        var renderer = Context.GetSystem<IRenderSystem>();

        var spriteMaterial = renderer.CreateMaterial("SpriteMaterial");
        var tilemapMaterial = renderer.CreateMaterial("TilemapMaterial");
        renderer.CreateMaterial("LightMaterial");

        {
            var spriteId = World.Create();
            World.Transforms2D.Add(spriteId, 
                new Transform2D(new Vector2(64, 64), new Vector2(64, 64), 0));
            World.Sprites.Add(spriteId, new SpriteEntity(1, spriteMaterial.Id, false));
        }

        {
            var spriteId = World.Create();
            World.Transforms2D.Add(spriteId, 
                new Transform2D(new Vector2(128, 128), new Vector2(64, 64), 0));
            World.Sprites.Add(spriteId, new SpriteEntity(2, spriteMaterial.Id, false)
            {
                SourceRectangle = new Rectangle<int>(0, 0, 64, 64)
            });
        }

        {
            var tilemapId = World.Create();
            World.Transforms2D.Add(tilemapId, 
                new Transform2D(Vector2.Zero, Vector2.One, 0));
            World.Tilemaps.Add(tilemapId, new TilemapEntity(tilemapMaterial.Id, 64, 32));
        }

        {
            var rng = new Random(1234);
            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 10; j++)
                {
                    var lightId = World.Create();
                    World.Lights.Add(lightId, new LightEntity
                    {
                        Position =  new Vector2(i * 256, j * 256),
                        Color = new Vector3(rng.NextSingle(),  rng.NextSingle(), rng.NextSingle()),
                        Intensity =  rng.NextSingle() * (3 - 1) + 1,
                        Radius = rng.Next(150,200)
                    });
                }
            }

        }
    }

    protected override void ConfigureFeatures(IGameSceneFeatureBuilder builder)
    {
        builder.RegisterDrawFeature<TilemapDrawEmitter, TilemapFeature, TilemapDrawData>(0);
        builder.RegisterDrawFeature<SpriteDrawEmitter, SpriteFeature, SpriteFeatureDrawData>(1);
        builder.RegisterDrawFeature<LightEmitter, LightFeature, LightFeatureDrawData>(2);
    }

    protected override void ConfigureModules(IGameSceneModuleBuilder builder)
    {
        builder.RegisterModule<RtsCameraModule>(0);
        builder.RegisterModule<NpcSpriteModule>(1);
    }

    protected override void ConfigureRenderer(IGameSceneRenderBuilder builder, IGraphicsDevice graphics)
    {
        builder.RegisterRenderer<DrawCommandMesh, SpriteRenderer>(DrawCommandTag.SpriteRenderer, DrawCommandId.Tilemap,
            DrawCommandId.Sprite);
        builder.RegisterRenderer<DrawCommandLight, LightRenderer>(DrawCommandTag.LightRenderer, DrawCommandId.Effect);

        builder.RegisterEmitter<TilemapDrawEmitter, TilemapDrawData>(0);
        builder.RegisterEmitter<SpriteDrawEmitter, SpriteFeatureDrawData>(1);
        builder.RegisterEmitter<LightEmitter, LightFeatureDrawData>(2);

        var assets = Context.GetSystem<IAssetSystem>();

        var lightPassShader = assets.Get<Shader>("LightPassShader");
        var lightComposite = assets.Get<Shader>("LightComposite");

        // single-sample scene FBO
        var sceneFboId =
            graphics.CreateFramebuffer(new FrameBufferDesc(SizeRatio: Vector2.One, DepthStencilBuffer: true),
                out var sceneFboMeta);

        var lightFboId =
            graphics.CreateFramebuffer(
                new FrameBufferDesc(SizeRatio: new Vector2(0.3f, 0.3f), TexturePreset: TexturePreset.NearestClamp,
                    DepthStencilBuffer: false),
                out var lightFboMeta);


        // colorTexId will be 0 for MSAA
        var msaaFboId = graphics.CreateFramebuffer(new FrameBufferDesc(
            SizeRatio: Vector2.One, DepthStencilBuffer: true, Msaa: true, Samples: 4), out _);

        // Pass 0: draw scene into MSAA FBO
        builder.RegisterRenderPass(RenderTargetId.Scene, 0, new SceneRenderPass
        {
            TargetFbo = msaaFboId,
            Clear = new RenderPassClearDesc(Color.CornflowerBlue, ClearBufferFlag.ColorAndDepth)
        });

        // Pass 1: resolve MSAA into single-sample texture FBO
        builder.RegisterRenderPass(RenderTargetId.Scene, 1, new BlitRenderPass
            {
                TargetFbo = sceneFboId,
                BlitFbo = msaaFboId,
                Multisample = true,
                Samples = 4
            }
        );

        // Pass 2: Draw light into FBO
        //SourceTexId = [lightFboMeta.ColTexId],
        builder.RegisterRenderPass(RenderTargetId.SceneLight, 2, new LightRenderPass
            {
                TargetFbo = lightFboId,
                Shader = lightPassShader.ResourceId,
                Clear = new RenderPassClearDesc(Color.FromArgb(255, 125, 125, 150), ClearBufferFlag.Color),
                Blend = BlendMode.Additive,
                DepthTest = false
            }
        );

        // Pass 3: Combine scene and light fbo texture into final scene
        builder.RegisterRenderPass(RenderTargetId.SceneLight, 3, new FsqRenderPass
        {
            TargetFbo = default,
            SourceTextures = [sceneFboMeta.ColTexId, lightFboMeta.ColTexId],
            Shader = lightComposite.ResourceId
        });
    }

    public override void Unload()
    {
    }
}