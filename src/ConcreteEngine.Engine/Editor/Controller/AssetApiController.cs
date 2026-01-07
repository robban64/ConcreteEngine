using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Store;
using ConcreteEngine.Editor.Store.Resources;
using ConcreteEngine.Engine.Assets;

namespace ConcreteEngine.Engine.Editor.Controller;

internal sealed class AssetApiController(ApiContext context) : IEngineAssetController
{
    private readonly AssetStore _store = context.AssetStore;

    public ReadOnlySpan<AssetObject> GetAssetSpan(AssetKind kind)
    {
        if (kind == AssetKind.Unknown) return ReadOnlySpan<AssetObject>.Empty;
        return _store.GetAssetList(kind).AssetObjectSpan;
    }

    public AssetFileSpec[] FetchAssetFileSpecs(AssetId assetId)
    {
        _store.TryGetFileIds(assetId, out var fileIds);

        if (fileIds.Length == 0 || !_store.TryGet(assetId, out _)) return [];

        var result = new AssetFileSpec[fileIds.Length];
        for (var i = 0; i < fileIds.Length; i++)
            _store.TryGetFileEntry(fileIds[i], out result[i]);

        return result;
    }

    public List<EditorAnimationResource> GetAnimationResources()
    {
        var span = context.World.AnimationTableImpl.ModelIdSpan;
        List<EditorAnimationResource> list = new(span.Length);
        _store.ExtractList<Model, EditorAnimationResource>(list, static (it) =>
        {
            if (it.AnimationId <= 0) return null!;
            var span = it.Animation!.ClipDataSpan;
            var clips = new EditorAnimationClip[span.Length];
            for (int i = 0; i < span.Length; i++)
            {
                var c = span[i];
                clips[i] = new EditorAnimationClip
                {
                    DisplayName = c.Name,
                    Duration = c.Duration,
                    TicksPerSecond = (float)c.TicksPerSecond,
                    TrackCount = c.Tracks.Count
                };
            }

            return new EditorAnimationResource
            {
                Name = it.Name,
                Id = new EditorId(it.AnimationId, EditorItemType.Animation),
                ModelId = new EditorId(it.ModelId, EditorItemType.Model),
                Clips = clips,
                Generation = 1
            };
        });


        return list;
    }
}