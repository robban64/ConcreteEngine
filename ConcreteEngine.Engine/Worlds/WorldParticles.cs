using System.Numerics;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Common.Numerics.Maths;
using ConcreteEngine.Common.Time;
using ConcreteEngine.Engine.Data;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Engine.Worlds.Render;
using ConcreteEngine.Engine.Worlds.Render.Batching;
using ConcreteEngine.Graphics.Gfx.Resources;
using ConcreteEngine.Renderer.Data;

namespace ConcreteEngine.Engine.Worlds;

public sealed class WorldParticles
{
    // public ModelId Model { get; private set; }
    public MeshId Mesh => _batcher.MeshId;
    public MaterialId Material { get; private set; }

    private ParticleBatcher _batcher;
    private MaterialTable _materialTable;
    private IMeshTable _meshTable;

    private ParticleStateData[] _particles = Array.Empty<ParticleStateData>();

    private float _translationTicker;
    private Vector3 _lastSampleTranslation;
    public Vector3 Translation {get; set;}
    private Vector3 StartArea {get; set;}
    private Vector3 Direction {get; set;}

    private ParticleDefinition _particleDef;

    internal WorldParticles()
    {
        _particleDef.StartColor = new Vector4(1.0f, 0.9f, 0.7f, 0.6f);
        _particleDef.EndColor = new  Vector4(1.0f, 0.9f, 0.6f, 0.05f);
        _particleDef.Gravity = new Vector3(0.001f, -0.2f, 0.001f);
        _particleDef.LifeMinMax = new Vector2(6f, 10f);
        _particleDef.SizeStartEnd = new Vector2(0.05f, 0.18f);
        _particleDef.SpeedMinMax = new Vector2(0.02f, 0.11f);

        StartArea = new Vector3(10, 1, 10);
        Direction = new Vector3(0, 1, 0);
    }

    public int ParticleCount => _particles.Length;
    public bool IsActive => Mesh > 0 && Material > 0;

    public void SetMaterial(MaterialId materialId) => Material = materialId;


    internal void AttachRenderer(ParticleBatcher batcher, MeshTable meshTable, MaterialTable materialTable)
    {
        _batcher = batcher;
        _meshTable = meshTable;
        _materialTable = materialTable;
    }

    public void CreateParticleMesh()
    {
        _batcher.BuildBatch();
        _meshTable.CreateSimpleModel(_batcher.MeshId, 0, 4, default);

        _particles = new ParticleStateData[_batcher.Capacity];
    }


    public void Simulate(float fixedDt, float totalTime, Vector3 cameraPos)
    {
        const float spread = 0.2f;
        _translationTicker += fixedDt;
        if (_translationTicker >= 1)
        {
            _lastSampleTranslation = cameraPos;
            _translationTicker = 0;
        }
        
        if(_lastSampleTranslation == default && cameraPos == default) return;
        
        Translation = Vector3.Lerp(_lastSampleTranslation, cameraPos, float.Min(_translationTicker, 1f));

        var rng = new FastRandom((uint)Environment.TickCount);

        var startArea = StartArea;
        var direction = Direction;
        var startPos = Translation;

        var gravityStep = _particleDef.Gravity * fixedDt;
        var particles = _particles.AsSpan();
        
        foreach (ref var particle in particles)
        {
            if (particle.Life < 0)
            {
                var offset = new Vector3(
                    rng.RandomFloat(-startArea.X, startArea.X), 
                    rng.RandomFloat(-startArea.Y, startArea.Y),
                    rng.RandomFloat(-startArea.Z, startArea.Z));
                    
                particle.Position = startPos + offset;
                particle.PrevPosition = particle.Position;
                particle.OriginalSpawnPos = particle.Position;
                particle.MaxLife = rng.RandomFloat(_particleDef.LifeMinMax);
                particle.Life = particle.MaxLife;

                var rngDir = new Vector3(rng.RandomFloat(-1f, 1f), rng.RandomFloat(-1f, 1f), rng.RandomFloat(-1f, 1f));
                rngDir = Vector3.Normalize(rngDir);
                particle.Velocity = Vector3.Normalize(direction + (rngDir * spread));
                continue;
            }
            
            var waveX = MathF.Sin(totalTime * 0.5f + particle.OriginalSpawnPos.Y); 
            var waveZ = MathF.Cos(totalTime * 0.3f + particle.OriginalSpawnPos.X); 
            var turbulence = new Vector3(waveX, 0, waveZ) * 0.1f;

            particle.PrevPosition = particle.Position;
            particle.Velocity += gravityStep;
            particle.Position += (particle.Velocity + turbulence) * fixedDt;
            particle.Life -= fixedDt;
        }

    }

    public void ProcessAndUpload(float alpha)
    {
        const float peakAlpha = 0.4f;

        var startColor = _particleDef.StartColor;
        var endColor = _particleDef.EndColor;

        var startEndSize = _particleDef.SizeStartEnd;
        
        var particles = _particles.AsSpan();
        var gpuParticles = _batcher.GetBufferSpan();
        for (var i = 0; i < particles.Length; i++)
        {
            ref var particle = ref particles[i];
            ref var gpuData = ref gpuParticles[i];
            
            var lifeRatio = 1f - (particle.Life / particle.MaxLife);

            var newPos = Vector3.Lerp(particle.PrevPosition, particle.Position, alpha);
            var newSize = float.Lerp(startEndSize.X, startEndSize.Y, lifeRatio);
            gpuData.PositionSize = new Vector4(newPos, newSize);
            
            float fadeCurve = 4.0f * lifeRatio * (1.0f - lifeRatio);
            gpuData.Color = Vector4.Lerp(startColor, endColor, lifeRatio);
            gpuData.Color.W = peakAlpha * fadeCurve; //;
        }

        _batcher.UploadGpuData();
    }
}