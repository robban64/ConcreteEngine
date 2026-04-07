namespace ConcreteEngine.Core.Common.Text;

public static class StringPacker
{
    public static uint PackUtf8(byte b0, byte b1, byte b2)
    {
        return (uint)(b0 | (b1 << 8) | (b2 << 16));
    }

    public static ulong PackAscii(ReadOnlySpan<char> s, bool ignoreCase = false)
    {
        ulong res = 0;
        var len = Math.Min(s.Length, 8);
        for (var i = 0; i < len; i++)
        {
            var c = ignoreCase ? char.ToLowerInvariant(s[i]) : s[i];
            res = (res << 8) | (byte)c;
        }

        return res << ((8 - len) * 8);
    }

    public static ulong PackAscii(ReadOnlySpan<byte> s, bool ignoreCase = false)
    {
        ulong res = 0;
        var len = Math.Min(s.Length, 8);
        for (var i = 0; i < len; i++)
        {
            var c = ignoreCase ? (byte)char.ToLowerInvariant((char)s[i]) : s[i];
            res = (res << 8) | c;
        }

        return res << ((8 - len) * 8);
    }

    public static ulong GetMaskUtf8(int length)
    {
        if (length <= 0) return 0;
        if (length >= 8) return ulong.MaxValue;
        return ulong.MaxValue << ((8 - length) * 8);
    }
}