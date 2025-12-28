using System.Text.Json;
using ConcreteEngine.Core.Diagnostics;
using ConcreteEngine.Engine.Assets.Internal;
using ConcreteEngine.Engine.Configuration.IO;
using ConcreteEngine.Engine.Diagnostics;

namespace ConcreteEngine.Engine.Configuration.IO;

internal static class EngineSettingsLoader
{
    public static void LoadGraphicSettings()
    {
        var path = EnginePath.GraphicSettingsFilePath;
        if (!File.Exists(path))
        {
            Logger.LogString(LogScope.Engine, "Loading Default Engine Settings...");
            EngineSettings.LoadSettings(new EngineSettingsRecord());
        }

        Logger.LogString(LogScope.Engine, "Loading Engine Settings...");

        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read,
            64 * 1024, FileOptions.SequentialScan);

        var options = JsonUtility.LightJsonOptions;
        var record = JsonSerializer.Deserialize<EngineSettingsRecord>(stream, options) ??
                     throw new InvalidDataException("Invalid Engine Settings.");

        EngineSettings.LoadSettings(record);
    }
}