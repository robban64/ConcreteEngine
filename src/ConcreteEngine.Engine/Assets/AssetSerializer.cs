using System.Text.Json;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Engine.Assets.Descriptors;
using ConcreteEngine.Engine.Configuration;

namespace ConcreteEngine.Engine.Assets;

internal static class AssetSerializer
{
    private static JsonSerializerOptions? _jsonOptions = JsonUtility.DefaultJsonOptions;

    public static AssetRecord LoadRecord(string path)
    {
        var text = File.ReadAllText(path);
        var record = JsonSerializer.Deserialize<AssetRecord>(text, _jsonOptions);
        if(record is null) Throwers.InvalidArgument(nameof(path), path);

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