using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Editor.Utils;

namespace ConcreteEngine.Editor.UI;

internal sealed class SceneBrowser
{
    private int _count;
    private SceneObjectId[] _sceneIds;
    private readonly CircularListBuffer<SceneItem> _buffer;

    public SceneBrowser()
    {
        _sceneIds = new SceneObjectId[512];
        _buffer = new CircularListBuffer<SceneItem>(64, (start, span) =>
        {
            var cursor = 0;
            foreach (var sceneObj in SceneManager.SceneStore.MakeSparseEnumerator(GetSceneIds(start, span.Length)))
            {
                ref var it = ref span[cursor++];
                it.Id = sceneObj.Id;
                it.Kind = sceneObj.Kind;
                it.Visible = sceneObj.Visible;
                it.DisplayName = sceneObj.Name;
            }
        });
    }

    public int FilteredCount => _count;
    public ReadOnlySpan<SceneObjectId> GetSceneIds(int start, int length) => new(_sceneIds, start, length);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public CircularListBuffer<SceneItem>.Enumerator GetDrawEnumerator(int start, int length) =>
        _buffer.GetView(start, length);

    public void Search(Span<byte> searchStr, SceneObjectKind selectedKind)
    {
        Ensure();
        
        var searchId = 0;
        ulong searchKey = 0, searchMask = 0;
        if (searchStr.Length > 0)
        {
            searchKey = StringPacker.PackAscii(searchStr, true);
            searchMask = StringPacker.GetMaskUtf8(searchStr.Length);
            if (!int.TryParse(searchStr, out searchId)) searchId = 0;
        }

        var count = 0;
        foreach (var it in SceneManager.SceneStore)
        {
            if (count >= EditorConsts.SceneCapacity) break;

            if (selectedKind > SceneObjectKind.Empty && selectedKind != it.Kind) continue;

            if (searchKey <= 0 || (it.PackedName & searchMask) == searchKey || searchId == it.Id.Id)
                _sceneIds[count++] = it.Id;
        }

        _count = count;
    }

    private void Ensure()
    {
        if (_count > 0) _sceneIds.AsSpan(0, _count).Clear();
        if (_sceneIds.Length < SceneManager.SceneStore.Count && _sceneIds.Length < EditorConsts.SceneCapacity)
        {
            var req = int.Min(SceneManager.SceneStore.Count, EditorConsts.SceneCapacity);
            var cap = CapacityUtils.CapacityGrowthToFit(_sceneIds.Length, req);
            _sceneIds = new  SceneObjectId[cap];
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct SceneItem
    {
        public SceneObjectId Id;
        public SceneObjectKind Kind;
        public bool Visible;
        public String16Utf8 DisplayName;
    }
}