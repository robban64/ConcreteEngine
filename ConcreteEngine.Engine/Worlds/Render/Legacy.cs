namespace ConcreteEngine.Engine.Worlds.Render;


/*
public void FlushEntities(DrawCommandBuffer buffer)
{
    if (_world is null || _entities.Length == 0 || _entityData.Length == 0) return;

    FlushWorldEntities(buffer);

    var prevModel = new ModelId(-1);
    var prevMatKey = new MaterialTagKey(-1);

    MaterialTag materialTag = default;
    ModelPartView modelView = default;
    ReadOnlySpan<MaterialId> matSpan = default;

    var len = _idx;

    if ((uint)len > _entities.Length || (uint)len > _entityData.Length)
        throw new IndexOutOfRangeException();

    var bufferContext = buffer.GetDrawUploaderCtx();

    for (var i = 0; i < len; i++)
    {
        ref readonly var entity = ref _entities[i];
        ref readonly var entityData = ref _entityData[i];

        if (entity.Source.Model != prevModel)
        {
            modelView = _meshTable.GetPartsRefView(entity.Source.Model);
            prevModel = entity.Source.Model;
        }

        if (entity.Source.MaterialKey != prevMatKey)
        {
            _materialTable.ResolveSubmitMaterial(entity.Source.MaterialKey, out materialTag);
            matSpan = materialTag.AsReadOnlySpan();
            prevMatKey = entity.Source.MaterialKey;
        }

        MatrixMath.CreateModelMatrix(entityData.Transform.Translation, entityData.Transform.Scale,
            entityData.Transform.Rotation, out var world);

        ref var mat0 = ref MemoryMarshal.GetReference(matSpan);

        var parts = modelView.Parts;
        var locals = modelView.Locals;

        var baseMeta = entity.Meta;
        var isAnimated = entity.Source.AnimatedSlot > 0;
        var animatedSlot = entity.Source.AnimatedSlot > 0 ? entity.Source.AnimatedSlot : (ushort)0;
        var localLen = int.Min(locals.Length, parts.Length);
        for (var partIdx = 0; partIdx < localLen; partIdx++)
        {
            ref readonly var part = ref parts[partIdx];

            ref var mat = ref Unsafe.Add(ref mat0, part.MaterialSlot);

            var isTransparent = materialTag.IsTransparent(part.MaterialSlot);
            var meta = BuildMeta(isTransparent, baseMeta);
            var cmd = new DrawCommand(part.Mesh, mat, drawCount: part.DrawCount, animationSlot: animatedSlot);

            ref var modelTransform = ref bufferContext.UploadDrawAndWrite(cmd, meta);
            ApplyTransform(ref modelTransform, in locals[partIdx], in world, isAnimated);
        }

        prevMatKey = entity.Source.MaterialKey;
        prevModel = entity.Source.Model;
    }
}
*/