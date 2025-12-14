#region

using System.Text;
using ConcreteEngine.Engine.Assets.Internal;

#endregion

namespace ConcreteEngine.Engine.Assets.Shaders;

internal sealed class ShaderImporter
{
    private const string Identifier = "@import ";

    private StringBuilder? _sb;

    private readonly Dictionary<string, (int, string)> _uboDict = new(8);
    private readonly Dictionary<string, string> _structsDict = new(4);

    private int _uboSlot;

    public void ImportAllDefinitions()
    {
        _sb ??= new StringBuilder(8192);
        _sb.Clear();

        ImportUboDefs(Path.Combine(AssetPaths.AssetCoreRoot, AssetPaths.ShaderFolder, "definitions", "ubo.glsl"));
        ImportStructDefs(Path.Combine(AssetPaths.AssetCoreRoot, AssetPaths.ShaderFolder, "definitions",
            "structs.glsl"));
        _sb.Clear();
    }

    private void ImportUboDefs(string path)
    {
        using var fs = File.OpenRead(path);
        using var sr = new StreamReader(fs, Encoding.UTF8);
        ParserMethods.ParseShaderDef(sr, this, "uniform", _sb!, UboCallback);
        _sb!.Clear();
    }

    private void ImportStructDefs(string path)
    {
        using var fs = File.OpenRead(path);
        using var sr = new StreamReader(fs, Encoding.UTF8);
        ParserMethods.ParseShaderDef(sr, this, "struct", _sb!, StructCallback);
    }

    public string ImportShader(string path, string? cacheName = null)
    {
        _sb ??= new StringBuilder(8192);
        _sb.Clear();

        using var fs = File.OpenRead(path);
        using var sr = new StreamReader(fs, Encoding.UTF8);

        var result = ParserMethods.ParseShader(sr, _sb, this, AppendUbo, AppendStruct);
        _sb.Clear();
        return result;
    }

    private static void StructCallback(string name, string content, ShaderImporter importer) =>
        importer._structsDict.Add(name, content);

    private static void UboCallback(string name, string content, ShaderImporter importer) =>
        importer._uboDict.Add(name, (importer._uboSlot++, content));

    private static void AppendUbo(string name, ShaderImporter importer)
    {
        (int slot, string content) = importer._uboDict[name];
        importer._sb!.Append($"layout(std140, binding = {slot}) ");
        importer._sb.Append(content);
        importer._sb.Append('\n');
    }

    private static void AppendStruct(string name, ShaderImporter importer)
    {
        importer._sb!.Append(importer._structsDict[name]);
        importer._sb.Append('\n');
    }

    public void ClearCache()
    {
        _sb = null;
        _uboDict.Clear();
        _structsDict.Clear();
        _uboSlot = 0;
    }

    private static class ParserMethods
    {
        public static string ParseShader(StreamReader sr, StringBuilder sb, ShaderImporter importer,
            Action<string, ShaderImporter> appendUbo, Action<string, ShaderImporter> appendStruct)
        {
            while (sr.ReadLine() is { } line)
            {
                var span = line.AsSpan();
                if (span.IsEmpty || span.StartsWith("//"))
                {
                    sb.Append('\n');
                    continue;
                }

                if (span.StartsWith(Identifier))
                {
                    span = span.Slice(Identifier.Length);
                    var s = span.Split(':');
                    var type = s.MoveNext() ? span[s.Current] : throw new InvalidOperationException();
                    var name = s.MoveNext() ? span[s.Current].ToString() : throw new InvalidOperationException();

                    switch (type)
                    {
                        case "ubo": appendUbo(name, importer); break;
                        case "struct": appendStruct(name, importer); break;
                        default: throw new InvalidOperationException(nameof(type));
                    }

                    continue;
                }

                var commentIdx = span.IndexOf("//", StringComparison.Ordinal);
                if (commentIdx > 0)
                {
                    sb.Append(span.Slice(0, commentIdx));
                    sb.Append('\n');
                    continue;
                }

                sb.Append(span);
                sb.Append('\n');
            }

            return sb.ToString();
        }

        public static void ParseShaderDef(
            StreamReader sr,
            ShaderImporter importer,
            string identifier,
            StringBuilder sb,
            Action<string, string, ShaderImporter> onAdd)
        {
            string? activeName = null;
            while (sr.ReadLine() is { } line)
            {
                var span = line.AsSpan();
                if (span.IsEmpty) continue;

                if (span.StartsWith(identifier))
                {
                    activeName = ExtractName(span);
                    sb.Append(span);
                    sb.Append('\n');
                }

                if (activeName == null) continue;

                var fieldEnd = span.IndexOf(";", StringComparison.OrdinalIgnoreCase);
                if (fieldEnd < 0) continue;

                sb.Append(span.Slice(0, fieldEnd + 1));
                sb.Append('\n');

                if (span.Contains("};", StringComparison.OrdinalIgnoreCase))
                {
                    if (activeName == null) throw new InvalidOperationException("Invalid shader def");

                    onAdd(activeName, sb.ToString(), importer);
                    activeName = null;
                    sb.Clear();
                }
            }
        }

        private static string ExtractName(ReadOnlySpan<char> line)
        {
            var s = line.SplitAny(ReadOnlySpan<char>.Empty);
            var type = s.MoveNext() ? line[s.Current] : ReadOnlySpan<char>.Empty;
            var name = s.MoveNext() ? line[s.Current] : ReadOnlySpan<char>.Empty;

            if (name.Length < 3)
                throw new InvalidOperationException("Shader def name require least 3 characters");

            return name.ToString();
        }
    }
}