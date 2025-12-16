using ConcreteEngine.Engine.Worlds.Entities;
using ConcreteEngine.Engine.Worlds.MeshGeneration;
using ConcreteEngine.Engine.Worlds.Render;
using ConcreteEngine.Engine.Worlds.Tables;
using ConcreteEngine.Engine.Worlds.View;

namespace ConcreteEngine.Engine.Worlds;

internal sealed class WorldContext
{
   public required WorldEntities Entities { get; init; }
   public required WorldSkybox Sky { get; init; }
   public required WorldTerrain Terrain { get; init; }
   public required WorldParticles Particles { get; init; }
   public required WorldRaycaster Raycast { get; init; }
   public required WorldRenderParams WorldRenderParams { get; init; }
   
   public required MeshGeneratorRegistry MeshGenerator { get; init; }
   public required MeshTable MeshTable { get; init; }
   public required MaterialTable MaterialTable { get; init; }
   public required AnimationTable AnimationTable { get; init; }
   public required Camera3D Camera { get; init; }

}