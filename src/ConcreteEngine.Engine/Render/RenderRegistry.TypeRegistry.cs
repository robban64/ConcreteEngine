using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Engine.Render.Passes;

// ReSharper disable StaticMemberInGenericType

namespace ConcreteEngine.Engine.Render;

public sealed partial class RenderRegistry
{
    public static int FboCount => Instance._fboCount;
    public static int PassCount => Instance._passCount;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static RenderFbo GetRenderFbo(PassId passId)
    {
        var fboId = GetPassEntry(passId).Params.Target;
        return Instance._frameBuffers[fboId];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static RenderPassEntry GetPassEntry(PassId passId) => Instance._passEntries[passId];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static RenderPassEntry GetPassEntry<TTarget>(FboVariant variant) where TTarget : unmanaged, IRenderTarget
    {
        var passId = TargetRegistry<TTarget>.GetPassId(variant);
        return Instance._passEntries[passId];
    }

    //
    private static int _targetCount;

    public static class TargetRegistry<TTarget> where TTarget : unmanaged, IRenderTarget
    {
        private static bool _isBound;
        public static byte TagIndex { get; private set; }

        private static InlineArray4<byte> _passIds;
        private static InlineArray4<FrameBufferId> _targetIds;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PassId GetPassId(FboVariant variant) => new(_passIds[variant]);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FboKey FboKey(FboVariant variant) => new(TagIndex, variant);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PassTargetKey PassKey(FboVariant variant) =>
            new(TagIndex, variant, new PassId(_passIds[variant]));

        public static PassTargetKey BindPassTarget(FboVariant variant, PassId passId)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(variant.Value, RenderLimits.MaxFboVariants);
            if (!_isBound) Throwers.NotFound(nameof(TTarget), "PassTag not registered.");
            if (_passIds[variant] != 0) Throwers.InvalidArgument(nameof(variant));

            var fboId = Instance.GetByKey(FboKey(variant)).FboId;

            _passIds[variant] = passId.Value;
            _targetIds[variant] = fboId;
            return PassKey(variant);
        }

        public static void RegisterTag()
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(_targetCount, RenderLimits.FboSlots);
            if (_isBound) Throwers.InvalidOperation("PassTag already registered.");
            TagIndex = (byte)_targetCount++;
            _isBound = true;
        }
    }
}