#region

using System.Numerics;
using ConcreteEngine.Common;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Core.Assets;
using ConcreteEngine.Core.Engine.Configuration;
using ConcreteEngine.Core.Rendering;
using ConcreteEngine.Core.Rendering.Descriptors;
using ConcreteEngine.Core.Scene;
using ConcreteEngine.Core.Scene.Entities;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Gfx.Definitions;
using Silk.NET.Maths;
using Shader = ConcreteEngine.Core.Assets.Shaders.Shader;

#endregion

namespace Demo2D;

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

    protected override void ConfigureModules(IGameSceneModuleBuilder builder)
    {
        //builder.RegisterModule<RtsCameraModule>(0);
        builder.RegisterModule<NpcSpriteModule>(0);
        builder.RegisterModule<DayNightModule>(1);
    }

    protected override void ConfigureRenderer(IGameSceneRenderBuilder builder)
    {
        var assets = Context.GetSystem<IAssetSystem>().Store;

        var lightPassShader = assets.GetByName<Shader>("LightPassShader");
        var lightComposite = assets.GetByName<Shader>("LightComposite");

        builder.RegisterRender2D(new RenderTargetDescriptor
        {
            SceneTarget = new SceneTargetDesc
            {
                ClearColor = Color4.CornflowerBlue,
                Samples = 4
            },
            LightTarget = new LightTargetDesc
            {
                LightShaderId = lightPassShader.ResourceId,
                Blend = BlendMode.Additive,
                ClearColor = Color4.FromRgba(125, 125, 150),
                SizeRatio = new Vector2(0.2f, 0.2f),
                TexPreset = TexturePreset.NearestClamp
            },
            ScreenTarget = new ScreenTargetDesc
            {
                ScreenShaderId = lightComposite.ResourceId
            }
        });
    }

    public override void Unload()
    {
    }
}