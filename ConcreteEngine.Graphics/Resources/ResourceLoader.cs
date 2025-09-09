using System.Runtime.InteropServices;
using ConcreteEngine.Graphics.Descriptors;

namespace ConcreteEngine.Graphics.Resources;
/*
internal sealed class ResourceLoader
{
    private enum ProcessOrder
    {
        NotStarted,
        Shaders,
        Textures,
        CubeMaps,
        Meshes
    }

    private const int DrainPerFrame = 16;
    private readonly IGraphicsDevice _graphics;
    private readonly GpuResourcePayloadCollection _payloadCollection;

    private ProcessOrder _processOrder = ProcessOrder.NotStarted;

    private int _idx = 0;

    public ResourceLoader(IGraphicsDevice graphics, GpuResourcePayloadCollection payloadCollection)
    {
        _graphics = graphics;
        _payloadCollection = payloadCollection;
    }


    public bool Process()
    {
        var pv = _payloadCollection;
        if (_processOrder == ProcessOrder.NotStarted) _processOrder  = ProcessOrder.Shaders;

        switch (_processOrder)
        {
            case ProcessOrder.NotStarted:
                break;
            case ProcessOrder.Shaders:
                ProcessShaders();
                _processOrder = ProcessOrder.Textures;
                break;
            case ProcessOrder.Textures:
                if (!ProcessTextures()) return false;
                _processOrder = ProcessOrder.CubeMaps;
                break;
            case ProcessOrder.CubeMaps:
                if (!ProcessCubeMaps()) return false;
                _processOrder = ProcessOrder.Meshes;
                break;
            case ProcessOrder.Meshes: 
                if (ProcessMeshes()) return true;
                break;
        }
        return false;
    }


    private void ProcessShaders()
    {
        var shaders = _payloadCollection.Shaders;
        var collection = shaders.Get();
        var result = new List<(ShaderId, ShaderMeta)>(collection.Count);
        foreach (var payload in collection)
        {
            var resourceId = _graphics.CreateShader(payload.VertexSource, payload.FragmentSource, out var meta);
            result.Add((resourceId, meta));
        }

        shaders.Callback(CollectionsMarshal.AsSpan(result));
    }

    private bool ProcessTextures()
    {
        var textures = _payloadCollection.Textures;

        while (textures.TryGet(out var queueIndex, out var payload))
        {
            var resourceId = _graphics.CreateTexture2D(payload, out var meta);
            textures.Callback(queueIndex, (resourceId, meta));
            _idx++;
            if (_idx == DrainPerFrame) return false;
        }

        _idx = 0;
        return true;
    }

    private bool ProcessCubeMaps()
    {
        var cubemaps = _payloadCollection.CubeMaps;

        while (cubemaps.TryGet(out var queueIndex, out var payload))
        {
            var resourceId = _graphics.CreateCubeMap(payload, out var meta);
            cubemaps.Callback(queueIndex, (resourceId, meta));
            _idx++;
            if (_idx == DrainPerFrame) return false;
        }
        
        _idx = 0;
        return true;
    }

    private bool ProcessMeshes()
    {
        var meshes = _payloadCollection.Meshes;

        while (meshes.TryGet(out var queueIndex, out var payload))
        {
            var resourceId = _graphics.CreateMesh(payload.GpuMeshData, payload.Descriptor, out var meta);
            meshes.Callback(queueIndex, (resourceId, meta));
            _idx++;
            if (_idx == DrainPerFrame) return false;
        }
        
        _idx = 0;
        return true;
    }
}*/