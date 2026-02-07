using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Text;

namespace ConcreteEngine.Editor.Utils;

internal unsafe struct SearchStringUtf8
{
    public const int Length = 8;

    private fixed byte _value[Length];
    private fixed char _searchString[Length];

    public ref byte GetInputRef() => ref _value[0];

    public Span<char> GetSearchString(out ulong key, out ulong mask)
    {
        key = 0;
        mask = 0;

        var length = UtfText.SliceNullTerminate(MemoryMarshal.CreateSpan(ref _value[0], Length), out var byteSpan);
        var dst = MemoryMarshal.CreateSpan(ref _searchString[0], length);
        if (!InputTextUtils.DecodeUtf8Input(byteSpan, dst, out var searchStr))
            return searchStr;

        key = StringPacker.PackUtf8(byteSpan);
        mask = StringPacker.GetMaskUtf8(length);
        return searchStr;
    }
}