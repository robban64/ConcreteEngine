namespace ConcreteEngine.Core.Common.Text;

public static class StringPacker
{
    public static ulong Pack(ReadOnlySpan<char> s)
    {
        ulong res = 0;
        int len = Math.Min(s.Length, 8);
        for (int i = 0; i < len; i++)
        {
            res = (res << 8) | (byte)s[i];
        }

        return res << ((8 - len) * 8);
    }

    public static ulong Pack(ReadOnlySpan<byte> s)
    {
        ulong res = 0;
        int len = Math.Min(s.Length, 8);
        for (int i = 0; i < len; i++)
        {
            res = (res << 8) | s[i];
        }

        return res << ((8 - len) * 8);
    }

    public static ulong GetMask(int length)
    {
        if (length <= 0) return 0;
        if (length >= 8) return ulong.MaxValue;
        return ulong.MaxValue << ((8 - length) * 8);
    }
}