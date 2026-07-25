using System.Text;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Text;
using static ConcreteEngine.Core.Engine.Configuration.EnginePath;

namespace ConcreteEngine.Engine.Assets.Importer;

internal sealed unsafe class ShaderImporter
{
    public const int ShaderBlockSize = 8192;
    public const int MinBlockSize = 4096;

    private static ReadOnlySpan<byte> Identifier => "@import "u8;
    private static ReadOnlySpan<byte> Std140Header => "layout(std140, binding = "u8;

    private sealed class UboDictEntry(int slot, byte[] textUtf8)
    {
        public readonly int Slot = slot;
        public readonly byte[] TextUtf8 = textUtf8;
    }

    private int _uboSlot;
    private readonly Dictionary<string, UboDictEntry> _uboDict = new(16);
    
    private readonly Dictionary<string, byte[]> _structsDict = new(4);

    public void ImportAllDefinitions()
    {
        var buffer = stackalloc byte[2048];
        var sw = new NativeSpanWriter(buffer, 1024);
        var line = new Span<byte>(buffer + 1024, 1024);
        ParseShaderDef(true, line, sw);
        ParseShaderDef(false, line, sw);
    }

    public ReadOnlySpan<byte> ImportShader(string path, NativeView<byte> buffer, out long length)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(buffer.Length, MinBlockSize, nameof(buffer));
        if (!File.Exists(path)) throw new FileNotFoundException("Shader Path not found.", path);

        using var fs = File.OpenRead(path);
        length = fs.Length;

        var sw = new NativeSpanWriter(buffer);
        Span<byte> line = stackalloc byte[1024];

        var cursor = 0;
        while (ReadLine(fs, line, ref cursor))
        {
            ParseShader(line.Slice(0, cursor), ref sw);
            cursor = 0;
        }

        if (cursor > 0)
            ParseShader(line.Slice(0, cursor), ref sw);

        return sw.EndSpan();
    }

    public void ClearCache()
    {
        _uboDict.Clear();
        _structsDict.Clear();
        _uboSlot = 0;
    }


    private void ParseShader(scoped Span<byte> line, scoped ref NativeSpanWriter sw)
    {
        if (sw.BytesLeft < line.Length || sw.BytesLeft < 16)
            Throwers.BufferOverflow("Insufficient memory for loading shader, increase limit");

        line = line.TrimWhitespace();
        if (line.IsEmpty || line.StartsWith("//"u8))
        {
            sw.Append('\n');
            return;
        }

        if (line.StartsWith(Identifier))
        {
            line = line.Slice(Identifier.Length);
            var s = line.Split((byte)':');
            var type = s.MoveNext() ? line[s.Current] : throw new InvalidOperationException();
            var name = s.MoveNext() ? line[s.Current] : throw new InvalidOperationException();
            var strName = Encoding.UTF8.GetString(name);

            if (type.SequenceEqual("ubo"u8))
            {
                var uboEntry = _uboDict[strName];
                sw.Append(Std140Header);
                sw.Append(uboEntry.Slot).Append((byte)')').Append(' ');
                sw.Append(uboEntry.TextUtf8);
                sw.Append('\n');
            }
            else if (type.SequenceEqual("struct"u8))
                sw.Append(_structsDict[strName]).Append('\n');
            else
                Throwers.InvalidOperation(nameof(type));

            return;
        }

        var commentIdx = line.IndexOf("//"u8);
        if (commentIdx > 0)
        {
            sw.Append(line.Slice(0, commentIdx)).Append('\n');
            return;
        }

        sw.Append(line).Append('\n');
    }


    public void ParseShaderDef(bool isUniform, Span<byte> line, NativeSpanWriter sw)
    {
        var filename = isUniform ? "ubo.glsl" : "structs.glsl";
        var identifier = isUniform ? "uniform"u8 : "struct"u8;

        using var fs = File.OpenRead(Path.Join(ShaderDefPath, filename));
        string? activeName = null;

        var cursor = 0;
        while (ReadLine(fs, line, ref cursor))
        {
            var span = line.Slice(0, cursor).TrimWhitespace();
            cursor = 0;

            if (span.IsEmpty) continue;

            if (span.StartsWith(identifier))
            {
                activeName = ExtractName(span);
                sw.Append(span);
                sw.Append('\n');
            }

            if (activeName == null) continue;

            var fieldEnd = span.IndexOf((byte)';');
            if (fieldEnd < 0) continue;

            sw.Append(span.Slice(0, fieldEnd + 1));
            sw.Append('\n');

            if (span.StartsWith((byte)'}') && fieldEnd > 0)
            {
                if (activeName == null!) Throwers.InvalidOperation("Invalid shader def");

                var result = sw.EndSpan().ToArray();
                if (isUniform) _uboDict.Add(activeName, new UboDictEntry(_uboSlot++, result));
                else _structsDict.Add(activeName, result);

                activeName = null;
                sw.Clear();
            }
        }

        if (cursor <= 0 || activeName == null) return;

        var lastLine = line.Slice(0, cursor);
        if (lastLine.StartsWith((byte)'}') && lastLine.EndsWith((byte)';'))
        {
            sw.Append(lastLine);
            var result = sw.EndSpan().ToArray();
            if (isUniform) _uboDict.Add(activeName, new UboDictEntry(_uboSlot++, result));
            else _structsDict.Add(activeName, result);
        }
    }

    private static bool ReadLine(FileStream fs, Span<byte> line, scoped ref int cursor)
    {
        int b;
        while ((b = fs.ReadByte()) != -1)
        {
            if (b == '\n')
            {
                if (cursor > 0 && line[cursor - 1] == '\r') cursor--;
                return true;
            }

            line[cursor++] = (byte)b;
        }

        return false;
    }

    private static string ExtractName(ReadOnlySpan<byte> line)
    {
        var s = line.SplitAny((byte)' ');
        _ = s.MoveNext() ? line[s.Current] : ReadOnlySpan<byte>.Empty;
        var name = s.MoveNext() ? line[s.Current] : ReadOnlySpan<byte>.Empty;

        if (name.Length < 3)
            Throwers.InvalidOperation("Shader def name require least 3 characters");

        return Encoding.UTF8.GetString(name);
    }
}