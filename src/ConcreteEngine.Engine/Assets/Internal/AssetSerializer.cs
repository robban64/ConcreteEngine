using System.Text.Json;
using ConcreteEngine.Engine.Assets.Descriptors;
using ConcreteEngine.Engine.Configuration.IO;

namespace ConcreteEngine.Engine.Assets.Internal;

internal static class AssetSerializer
{
    private static JsonSerializerOptions? _jsonOptions = JsonUtility.DefaultJsonOptions;

    public static AssetRecord LoadRecord(string path)
    {
        var text = File.ReadAllText(path);
        var record = JsonSerializer.Deserialize<AssetRecord>(text, _jsonOptions) ??
                     throw new InvalidDataException("Invalid file.");

        return record;
    }

    public static void WriteRecord(string path, AssetRecord record)
    {
        var text = JsonSerializer.Serialize(record, _jsonOptions) ??
                   throw new InvalidDataException("Invalid record.");
        File.WriteAllText(path, text);
    }


    public static void ClearCache() => _jsonOptions = null;

    /*
             using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read,
           64 * 1024, FileOptions.SequentialScan);

       var manifest = JsonSerializer.Deserialize<T>(fs, _jsonOptions)
                      ?? throw new InvalidDataException($"Invalid resource manifest for {typeof(T).Name}.");

     */

}