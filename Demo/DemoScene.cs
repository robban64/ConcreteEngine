#region

using System.Numerics;
using ConcreteEngine.Common;
using ConcreteEngine.Core.Assets;
using ConcreteEngine.Core.Configuration;
using ConcreteEngine.Core.Features;
using ConcreteEngine.Core.Rendering;
using ConcreteEngine.Core.Scene;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Definitions;
using Silk.NET.Maths;
using Shader = ConcreteEngine.Core.Resources.Shader;

#endregion

namespace Demo;

public sealed class DemoScene : GameScene
{
    public override void Initialize()
    {
        var renderer = Context.GetSystem<IRenderSystem>();

        var spriteMaterial = renderer.CreateMaterial("SpriteMaterial");
        var spriteMaterial2 = renderer.CreateMaterial("SpriteMaterial");

        var tilemapMaterial = renderer.CreateMaterial("TilemapMaterial");
        renderer.CreateMaterial("LightMaterial");

        int currSpriteId = 1;
        for (int x = 0; x < 4; x++)
        {
            for (int y = 0; y < 4; y++)
            {
                for (int i = 0; i < 64; i++)
                {
                    var spriteId = World.Create();
                    var offsetX = MathF.Cos(i * MathF.PI / 64f) + i;
                    var offsetY = MathF.Sin(i * MathF.PI / 64f) + i;

                    var t = new Transform2D(new Vector2(512 * x + offsetX, 512 * y + offsetY), new Vector2(64, 64), 0);

                    World.Transforms2D.Add(spriteId, t);
                    World.PrevTransforms2D.Add(spriteId, t);

                    World.Sprites.Add(spriteId, new SpriteComponent(currSpriteId++, spriteMaterial.Id, false)
                    {
                        SourceRectangle = new Rectangle<int>(0, 0, 64, 64)
                    });
                }
            }
        }

/*
        {
            var spriteId = World.Create();
            World.Transforms2D.Add(spriteId,
                new Transform2D(new Vector2(64, 64), new Vector2(64, 64), 0));
            World.Sprites.Add(spriteId, new SpriteComponent(1, spriteMaterial.Id, false));
        }

        int currSpriteId = 2;
        for (int x = 0; x < 22; x++)
        {
            for (int y = 0; y < 22; y++)
            {
                var spriteId = World.Create();
                World.Transforms2D.Add(spriteId,
                    new Transform2D(new Vector2(64 * x + 64, 64 * y + 64), new Vector2(64, 64), 0));
                World.Sprites.Add(spriteId, new SpriteComponent(currSpriteId++, spriteMaterial.Id, false)
                {
                    SourceRectangle = new Rectangle<int>(0, 0, 64, 64)
                });
            }
        }

        int offset = 32;
        for (int x = 0; x < 22; x++)
        {
            for (int y = 0; y < 22; y++)
            {
                var spriteId = World.Create();
                World.Transforms2D.Add(spriteId,
                    new Transform2D(new Vector2(64 * x + offset, 64 * y + offset), new Vector2(64, 64), 0));
                World.Sprites.Add(spriteId, new SpriteComponent(currSpriteId++, spriteMaterial2.Id, false)
                {
                    SourceRectangle = new Rectangle<int>(0, 0, 64, 64)
                });
            }
        }



*/

        {
            var tilemapId = World.Create();
            World.Transforms2D.Add(tilemapId,
                new Transform2D(Vector2.Zero, Vector2.One, 0));
            World.Tilemaps.Add(tilemapId, new TilemapComponent(tilemapMaterial.Id, 64, 32));
        }

        {
            var rng = new Random(1234);
            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 10; j++)
                {
                    var lightId = World.Create();
                    World.Lights.Add(lightId, new LightComponent
                    {
                        Position = new Vector2(i * 256, j * 256),
                        Color = new Vector3(rng.NextSingle(), rng.NextSingle(), rng.NextSingle()),
                        Intensity = rng.NextSingle() * (3 - 1) + 1,
                        Radius = rng.Next(150, 200)
                    });
                }
            }
        }
    }

    protected override void ConfigureFeatures(IGameSceneFeatureBuilder builder)
    {
        builder.RegisterDrawFeature<TilemapDrawProducer, TilemapFeature, TilemapDrawData>(0);
        builder.RegisterDrawFeature<SpriteDrawProducer, SpriteFeature, SpriteFeatureDrawData>(1);
        builder.RegisterDrawFeature<LightProducer, LightFeature, LightFeatureDrawData>(2);
    }

    protected override void ConfigureModules(IGameSceneModuleBuilder builder)
    {
        builder.RegisterModule<RtsCameraModule>(0);
        builder.RegisterModule<NpcSpriteModule>(1);
        builder.RegisterModule<DayNightModule>(2);
    }

    protected override void ConfigureRenderer(IGameSceneRenderBuilder builder, IGraphicsDevice graphics)
    {
        builder.RegisterRenderer<DrawCommandMesh, SpriteRenderer>(DrawCommandTag.SpriteRenderer, DrawCommandId.Tilemap,
            DrawCommandId.Sprite);
        builder.RegisterRenderer<DrawCommandLight, LightRenderer>(DrawCommandTag.LightRenderer, DrawCommandId.Effect);

        builder.RegisterDrawProducer<TilemapDrawProducer, TilemapDrawData>(0);
        builder.RegisterDrawProducer<SpriteDrawProducer, SpriteFeatureDrawData>(1);
        builder.RegisterDrawProducer<LightProducer, LightFeatureDrawData>(2);

        var assets = Context.GetSystem<IAssetSystem>();

        var lightPassShader = assets.Get<Shader>("LightPassShader");
        var lightComposite = assets.Get<Shader>("LightComposite");

        builder.RegisterRenderTargets(new RenderTargetDescription
        {
            SceneTarget = new SceneTargetDesc
            {
                ClearColor = Colors.CornflowerBlue,
                Samples = 4
            },
            LightTarget = new LightTargetDesc
            {
                LightShader = lightPassShader.ResourceId,
                Blend = BlendMode.Additive,
                ClearColor = Color4.FromRgba(125, 125, 150),
                SizeRatio = new Vector2(0.2f, 0.2f),
                TexPreset = TexturePreset.NearestClamp
            },
            ScreenTarget = new ScreenTargetDesc
            {
                CompositeShaderId = lightComposite.ResourceId
            }
        });
    }

    public override void Unload()
    {
    }
}