using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Renderer;
using ConcreteEngine.Graphics.Gfx.Handles;

namespace ConcreteEngine.Core.Engine.Graphics;

public sealed class Terrain
{
    private const int TerrainHeight = 12;
    private const int TerrainStep = 1;

    public MeshId Mesh { get; internal set; }
    public MaterialId Material { get; private set; }
    
    private AssetId _heightmapId;

    private float[] _heights = [];
    
    public bool IsDirty { get; private set; }
    public int MaxHeight { get; private set; } = TerrainHeight;
    public int Step { get; private set; } = TerrainStep;
    public int Dimension { get; private set; }
    public int Size { get; private set; }


    internal Terrain()
    {
    }

    public bool HasHeightmap => _heights.Length > 0 && _heightmapId.IsValid();
    public void SetMaterial(MaterialId materialId) => Material = materialId;

    public void CreateFrom(Texture heightmap)
    {
        if(!heightmap.PixelData.HasValue)
            throw new ArgumentNullException(nameof(heightmap));
        
        ArgumentNullException.ThrowIfNull(heightmap);
        ArgumentOutOfRangeException.ThrowIfEqual(heightmap.PixelData.HasValue, false, nameof(heightmap));
        ArgumentOutOfRangeException.ThrowIfLessThan(heightmap.Size.Width, 64, nameof(heightmap));
        ArgumentOutOfRangeException.ThrowIfNotEqual(heightmap.Size.Width, heightmap.Size.Height, nameof(heightmap));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(Step, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(Step, 16);

        _heightmapId = heightmap.Id;
        Dimension = heightmap.Size.Width;
        Size = heightmap.Size.Width * heightmap.Size.Width;

        BuildHeightMap(heightmap.PixelData.Value.Span);

        IsDirty = true;
    }

    private void BuildHeightMap(ReadOnlySpan<byte> data)
    {
        var width = Dimension;
        var maxHeight = MaxHeight;
        var size = width * width;
        if(_heights.Length < size)
            _heights = new float[size];

        for (int z = 0; z < width; z++)
        {
            int rowStart = z * width;
            for (int x = 0; x < width; x++)
            {
                _heights[rowStart + x] = SampleHeight(data, x, z, width, maxHeight);
            }
        }

    }
    

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float GetHeight(int x, int z)
    {
        x = Math.Clamp(x, 0, Dimension - 1);
        z = Math.Clamp(z, 0, Dimension - 1);
        return _heights[z * Dimension + x];
    }

    public float GetSmoothHeight(float x, float z)
    {
        var ix = int.Clamp((int)x, 0, Dimension);
        var iz = int.Clamp((int)z, 0, Dimension);

        float gridSquareSize = Dimension / ((float)Size - 1);
        float xCord = x % gridSquareSize / gridSquareSize;
        float zCord = z % gridSquareSize / gridSquareSize;
        if (xCord <= 1 - zCord)
        {
            return VectorMath.BarryCentric(
                new Vector3(0, GetHeight(ix, iz), 0),
                new Vector3(1, GetHeight(ix + 1, iz), 0),
                new Vector3(0, GetHeight(ix, iz + 1), 1),
                new Vector2(zCord, xCord));
        }

        return VectorMath.BarryCentric(
            new Vector3(1, GetHeight(ix + 1, iz), 0),
            new Vector3(1, GetHeight(ix + 1, iz + 1), 1),
            new Vector3(0, GetHeight(ix, iz + 1), 1),
            new Vector2(xCord, zCord));
    }

    public Vector3 GetPointOnTerrainPlane(in Ray ray)
    {
        var n = Vector3.UnitY;
        Vector3 p0 = default;

        var numerator = Vector3.Dot(p0 - ray.Position, n);
        var denominator = Vector3.Dot(ray.Direction, n);

        if (Math.Abs(denominator) < 1e-6f)
            return Vector3.Zero;

        var t = numerator / denominator;
        if (t < 0) return Vector3.Zero;
        var pointOnPlane = ray.GetPointOnRay(t);

        if (pointOnPlane.X < 0 || pointOnPlane.Z < 0 || pointOnPlane.X > Dimension || pointOnPlane.Z > Dimension)
            return Vector3.Zero;

        var terrainHeight = GetSmoothHeight(pointOnPlane.X, pointOnPlane.Z);

        if (Math.Abs(pointOnPlane.Y - terrainHeight) > TerrainHeight)
            return Vector3.Zero;

        return pointOnPlane with { Y = terrainHeight };
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float SampleHeight(ReadOnlySpan<byte> data, int x, int z, int dimension, int maxHeight)
    {
        const int channels = 4;

        x = Math.Clamp(x, 0, dimension - 1);
        z = Math.Clamp(z, 0, dimension - 1);
        
        var rowStrideBytes = data.Length / dimension;

        var idx = z * rowStrideBytes + x * channels;
        if ((uint)(idx + channels - 1) >= (uint)data.Length)
            return 0f;

        byte r = data[idx];
        return r / 255f * maxHeight;
    }
}