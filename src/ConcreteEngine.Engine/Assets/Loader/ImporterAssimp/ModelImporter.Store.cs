namespace ConcreteEngine.Engine.Assets.Loader.ImporterAssimp;

using static AssimpUtils;

internal sealed partial class ModelImporter
{
    private static uint[] _hashes = null!;
    private static short[] _boneIndices = null!;
    private static IntPtr[] _nodes = null!;
    private static int _hashIndex;

    private static void InitStore()
    {
        _hashes = new uint[BoneLimit * 2];
        _boneIndices = new short[BoneLimit * 2];
        _nodes = new IntPtr[BoneLimit * 2];
        _hashIndex = 0;
    }

    private static void ClearStore()
    {
        Array.Clear(_hashes);
        Array.Clear(_nodes);
        _boneIndices.AsSpan().Fill(-2);
        _hashIndex = 0;
    }

    private static void DisposeStore()
    {
        _hashes = null!;
        _boneIndices = null!;
        _nodes = null!;
        _hashIndex = 0;
    }

    private static bool TryGetBoneIndex(uint hash, out int boneIndex)
    {
        var idx = _hashes.IndexOf(hash);
        boneIndex = idx >= 0 ? _boneIndices[idx] : -1;
        return boneIndex >= 0;
    }

    private static bool TryGetNode(uint hash, out IntPtr nodePtr)
    {
        var idx = _hashes.IndexOf(hash);
        nodePtr = idx >= 0 ? _nodes[idx] : -1;
        return nodePtr > 0;
    }
}