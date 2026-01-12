using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Graphics.Diagnostic;

namespace ConcreteEngine.Editor.Metrics;

public static partial class MetricsApi
{
    public static class Provider<T> where T : unmanaged
    {
        internal static T Data;
        internal static PollMetricProvider<T>? Record;

        public static void Register(int intervalTicks, FuncFill<T> fetch)
        {
            if (HasInitialized) throw new InvalidOperationException("MetricApi is initialized");
            if (Record != null) All.Remove(Record);
            Record = new PollMetricProvider<T>(intervalTicks, fetch);
            All.Add(Record);
        }
    }

    public static class Store
    {
        internal static StoreMetricProvider<GfxStoreMeta>? Gfx;
        internal static StoreMetricProvider<AssetStoreMeta>? Assets;

        private static string[] _gfxMetaDescriptions = [];

        internal static ReadOnlySpan<string> GfxMetaDescriptions => _gfxMetaDescriptions;

        internal static void Toggle(bool enabled)
        {
            Gfx?.Toggle(enabled);
            Assets?.Toggle(enabled);
            if (!enabled) _gfxMetaDescriptions.AsSpan().Clear();
        }

        public static void RegisterGfx(int count, Action<Span<GfxStoreMeta>> onRequestRefresh)
        {
            if (Gfx is not null) throw new InvalidOperationException("MetricApi GfxStore already initialized");
            Gfx = new StoreMetricProvider<GfxStoreMeta>(count, onRequestRefresh) { OnDataChange = OnGfxDataChange };
            _gfxMetaDescriptions = new string[count];
        }

        public static void RegisterAsset(int count, Action<Span<AssetStoreMeta>> onRequestRefresh)
        {
            if (Assets is not null) throw new InvalidOperationException("MetricApi GfxStore already initialized");
            Assets = new StoreMetricProvider<AssetStoreMeta>(count, onRequestRefresh);
        }

        private static void OnGfxDataChange()
        {
            for (var i = 0; i < _gfxMetaDescriptions.Length; i++)
                _gfxMetaDescriptions[i] = MetricsFormatter.FormatGfxStoreMeta(in Gfx!.GetData()[i]);
        }
    }
}