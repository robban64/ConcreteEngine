using System.Text.Json;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Engine.Editor.Diagnostics;

namespace ConcreteEngine.Engine.Configuration.IO;

internal static class EngineSettingsLoader
{
    public static void LoadGraphicSettings()
    {
        if (!Directory.Exists(EnginePath.ConfigRoot)) Directory.CreateDirectory(EnginePath.ConfigRoot);

        var options = JsonUtility.LightJsonOptions;

        var path = EnginePath.GraphicSettingsFilePath;
        if (!File.Exists(path))
        {
            Logger.LogString(LogScope.Engine, "Loading Default Engine Settings...");
            EngineSettings.LoadSettings(new EngineSettingsRecord());

            var tempRecord = EngineSettings.Instance.GetSettingsRecord();
            var str = JsonSerializer.Serialize(tempRecord, options) ??
                      throw new InvalidDataException("Invalid Engine Settings.");
            File.WriteAllText(path, str);

            Logger.LogString(LogScope.Engine, "Created config/graphic settings.json");
            Logger.LogString(LogScope.Engine, tempRecord.Display.ToString(), LogLevel.Info);
            Logger.LogString(LogScope.Engine, tempRecord.Simulation.ToString(), LogLevel.Info);
            Logger.LogString(LogScope.Engine, tempRecord.GraphicsQuality.ToString(), LogLevel.Info);
        }

        Logger.LogString(LogScope.Engine, "Loading Engine Settings...");

        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read,
            64 * 1024, FileOptions.SequentialScan);

        var record = JsonSerializer.Deserialize<EngineSettingsRecord>(stream, options) ??
                     throw new InvalidDataException("Invalid Engine Settings.");

        EngineSettings.LoadSettings(record);
    }
}