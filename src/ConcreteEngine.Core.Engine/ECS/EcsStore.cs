using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Core.Engine.ECS.Integration;

namespace ConcreteEngine.Core.Engine.ECS;

public enum EcsStoreType : byte
{
    Unknown,
    Render,
    RenderCore,
    Game,
    GameCore
}

public abstract class EcsStore : IDisposable
{
    private static int _currentStoreId;

    protected sealed class EcsStoreMeta
    {
        public readonly int StoreId = ++_currentStoreId;
        public bool IsDirty;
        public readonly List<Action<EcsStore>> OnResizeCallbacks = [];
        public readonly List<IEntityListener> Listeners = [];
    }

    protected readonly EcsStoreMeta StoreMeta = new();
    protected readonly Stack<int> Free = [];

    public int Count { get; protected set; }
    public int ActiveCount => Count - Free.Count;
    public bool IsDirty => StoreMeta.IsDirty;
    public int StoreId => StoreMeta.StoreId;

    public abstract int Capacity { get; }
    public abstract EcsStoreType StoreType { get; }

    public void AddResizeCallback(Action<EcsStore> callback) => StoreMeta.OnResizeCallbacks.Add(callback);
    public void RemoveResizeCallback(Action<EcsStore> callback) => StoreMeta.OnResizeCallbacks.Remove(callback);

    public void BindListener(IEntityListener listener) => StoreMeta.Listeners.Add(listener);
    public void UnbindListener(IEntityListener listener) => StoreMeta.Listeners.Remove(listener);

    protected abstract void Resize(int newSize);

    protected int AllocateNext()
    {
        var index = SlotHelper.NextSlot(Free, Count);
        if (index >= 0) return index;

        if (Count >= Capacity) EnsureCapacity(1);
        return Count++;
    }

    protected void FreeEntity(int index)
    {
        StoreMeta.IsDirty = true;
        Count = SlotHelper.FreeSlot(Free, index, Count);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void EnsureCapacity(int amount)
    {
        var len = Count + amount;
        if (Capacity >= len) return;

        var newSize = CapacityUtils.CapacityGrowthSafe(Capacity, len);
        newSize = IntMath.AlignUp(newSize, 32);

        Resize(newSize);

        foreach (var callback in StoreMeta.OnResizeCallbacks)
            callback(this);

        Logger.LogString(LogScope.Ecs, $"GameEntities: resized {newSize}", LogLevel.Warn);
    }

    public abstract void Dispose();
}